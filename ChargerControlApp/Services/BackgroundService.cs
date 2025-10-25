﻿using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ChargerControlApp.Services.InitialState;

namespace ChargerControlApp.Services
{
    public class CanBusPollingService : BackgroundService
    {
        private readonly HardwareManager _hardwareManager;
        //private readonly NPB1700Controller _npbController;
        private readonly ILogger<CanBusPollingService> _logger;
        //private readonly IServiceProvider _serviceProvider;
        private readonly ChargingStationStateMachine _chargingStationStateMachine;
        private readonly RobotService _robotService;

        //public CanBusPollingService(NPB1700Controller npbController, ILogger<CanBusPollingService> logger)
        //{
        //    _npbController = npbController;
        //    _logger = logger;
        //}

        public CanBusPollingService(IServiceProvider serviceProvider)
        {
            _hardwareManager = serviceProvider.GetService<HardwareManager>();
            _logger = serviceProvider.GetService<ILogger<CanBusPollingService>>();
            _chargingStationStateMachine = serviceProvider.GetService<ChargingStationStateMachine>();
            //_npbController = serviceProvider.GetService<NPB1700Controller>();
            _robotService = serviceProvider.GetService<RobotService>();
        }
        //public CanBusPollingService(HardwareManager hardwareManager, ILogger<CanBusPollingService> logger)
        //{
        //    _hardwareManager = hardwareManager;
        //    _logger = logger;
        //}
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("背景應用模組 啟動");

            // 初始化 Charger 狀態
            for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
            {
                if (i < HardwareManager.NPB450ControllerInstnaceNumber)
                {
                    _hardwareManager.Charger[i].IsUsed = true;
                    if(_hardwareManager.SlotServices.SlotInfo[i].State.CurrentState.GetStateEnum() == SlotState.NotUsed)
                    {
                        _hardwareManager.SlotServices.TransitionTo(i, state: SlotState.Initialization);
                        _hardwareManager.Charger[i].StopCharging(); // 停止充電
                    }
                }
                else
                {
                    if (_hardwareManager.SlotServices.SlotInfo[i].State.CurrentState.GetStateEnum() != SlotState.NotUsed)
                        _hardwareManager.SlotServices.TransitionTo(i, state: SlotState.NotUsed);
                }
                _hardwareManager.SlotServices.TransferToSlotChargeState(i); // 同步更新充電狀態
            }

            if (HardwareManager.ServoOnAndHomeAfterStartup) // 啟動後是否啟用伺服並回原點
            {
                try
                {
                    _hardwareManager.Robot.SetAllServo(true);
                    await Task.Delay(2000);
                    _robotService.StartHomeProcedure();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "機械手臂伺服啟動或回原點失敗");
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    for (int i = 0; i < HardwareManager.NPB450ControllerInstnaceNumber; i++)
                    {
                        SlotStateCheck(i);
                        
                        try
                        {
                            if (NPB450Controller.ChargerUseAsync)
                                await _hardwareManager.Charger[i].PollingOnceAsync(); // 使用非同步版本
                            else
                                await Task.Run(() => _hardwareManager.Charger[i].PollingOnce()); // 使用同步版本
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Charger[{i}] PollingOnce 發生例外");
                        }
                        await Task.Delay(20);
                    }

                    // Read GPIO Inputs
                    GPIOService.ReadInputsFromHardware();

                    //_hardwareManager.Charger.PollingOnce();
                    //_logger.LogDebug("輪詢成功，電壓: {Voltage}", _hardwareManager.Charger.GetCachedVoltage());

                    /*
                    //State monitoring
                    if (_chargingStationStateMachine._currentState is IdleState ||
                        _chargingStationStateMachine._currentState is ReservedState)
                    {
                        if (_hardwareManager.Charger[0].GetCachedVoltage() >= 10)
                        {
                            _chargingStationStateMachine.TransitionTo<OccupiedState>();
                        }
                    }
                    if (_chargingStationStateMachine._currentState is OccupiedState)
                    {
                        if (_hardwareManager.Charger[0].GetCachedVoltage() < 10)
                        {
                            _chargingStationStateMachine.TransitionTo<IdleState>();
                        }
                    }
                    if (_chargingStationStateMachine._currentState is ChargingStateClass)
                    {
                        if (_hardwareManager.Charger[0].GetCachedVoltage() >= 57.5 &&
                            _hardwareManager.Charger[0].GetCachedCurrent() < 2.5)
                        {
                            _chargingStationStateMachine.TransitionTo<OccupiedState>();
                        }
                    }
                    */
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "輪詢 NPB450 發生錯誤");
                }

                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("CanBusPollingService 結束");
        }

        /// <summary>
        /// Slot 狀態檢查與轉換
        /// </summary>
        /// <param name="index"></param>
        protected void SlotStateCheck(int index)
        {
            var charger = _hardwareManager.Charger[index];
            var slotState = _hardwareManager.SlotServices.SlotInfo[index];
            var slotService = _hardwareManager.SlotServices;
            if (slotState.IsEnabled)
            {
                // 依 Charger 錯誤訊息決定 Slot 狀態
                if ((charger.FAULT_STATUS.Data != 0) && (slotState.State.CurrentState.CurrentState != SlotState.SupplyError))
                    slotService.TransitionTo(index, SlotState.SupplyError);

                // 從Slot狀態錯誤決定 Slot 狀態
                if (slotState.StateError && (charger.FAULT_STATUS.Data == 0) && (slotState.State.CurrentState.CurrentState != SlotState.StateError))
                    slotService.TransitionTo(index, SlotState.StateError);

                if (slotState.StateError && (charger.FAULT_STATUS.Data != 0)) return;

                // 狀態轉換邏輯   
                if (slotState.State.CurrentState.CurrentState == SlotState.Initialization)
                {
                    // 初始化完成，轉到 Empty 狀態(無電池) 或 Idle 狀態(有電池)
                    if (slotState.BatteryMemory)
                        slotService.TransitionTo(index, SlotState.Idle);
                    else
                        slotService.TransitionTo(index, SlotState.Empty);
                }
                else if (slotState.State.CurrentState.CurrentState == SlotState.Idle)
                {
                    // 有電池，等待充電，下達充電指令
                    charger.StartCharging();
                    slotService.TransitionTo(index, SlotState.Charging);
                }
                else if (slotState.State.CurrentState.CurrentState == SlotState.Charging)
                {
                    // 充電中，檢查是否浮充
                    if (charger.CHG_STATUS.Bits.FVM)
                        slotService.TransitionTo(index, SlotState.Floating);
                }
            }
        }

    }

    public class GrpcBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _mainProvider;
        private IHost? _grpcHost;

        public GrpcBackgroundService(IServiceProvider mainProvider)
        {
            _mainProvider = mainProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ gRPC Background Service 啟動中...");

            _grpcHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 50051, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddGrpc();

                        // 👇 用工廠方式從原本的 DI 拿服務
                        services.AddSingleton(provider =>
                            _mainProvider.GetRequiredService<ChargingStationStateMachine>());
                        //services.AddSingleton(provider =>
                        //    _mainProvider.GetRequiredService<ReservedState>());
                        //services.AddSingleton(provider =>
                        //    _mainProvider.GetRequiredService<ChargingStateClass>());
                        services.AddSingleton(provider =>
                            _mainProvider.GetRequiredService<SwappingStationService>());
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGrpcService<SwappingStationService>();
                            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                        });
                    });
                })
                .Build();

            await _grpcHost.StartAsync(stoppingToken);
            Console.WriteLine("✅ gRPC 已啟動於 Port 50051");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_grpcHost != null)
            {
                await _grpcHost.StopAsync(cancellationToken);
                _grpcHost.Dispose();
            }
        }
    }

    public class ModbusPollingService : BackgroundService
    {
        private readonly HardwareManager _hardwareManager;
        private readonly ILogger<ModbusPollingService> _logger;
        private readonly AppSettings _settings;

        public ModbusPollingService(IServiceProvider serviceProvider)
        {
            _hardwareManager = serviceProvider.GetService<HardwareManager>();
            _logger = serviceProvider.GetService<ILogger<ModbusPollingService>>();
            _settings = serviceProvider.GetRequiredService<AppSettings>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ModbusPollingService 啟動");
            _hardwareManager.Robot.Open();
            string[] portnamelist = System.IO.Ports.SerialPort.GetPortNames();

            string portName = _hardwareManager.modbusRTUService.PortName;

#if RELEASE
        portName = _settings.PortNameLinux;
#endif

            _hardwareManager.modbusRTUService.PortName = portName;

            _logger.LogInformation($"Available COM Ports: {string.Join(", ", portnamelist)}");
            try
            {
                _hardwareManager.modbusRTUService.Open();
                _logger.LogInformation($"To open port {_hardwareManager.modbusRTUService.PortName} Success...");
            }
            catch (ModbusRTUServiceException ex)
            {
                // auto select one available port
                Console.WriteLine($"Failed to open port {ex.PortName}: {ex.Message}");
                if (portnamelist.Length > 0)
                {
                    _hardwareManager.modbusRTUService.PortName = portnamelist[0];
                    Console.WriteLine($"Trying to open port {_hardwareManager.modbusRTUService.PortName}...");
                    try
                    {
                        _hardwareManager.modbusRTUService.Open();
                    }
                    catch (ModbusRTUServiceException ex2)
                    {
                        Console.WriteLine($"Failed to open port {ex2.PortName} again: {ex2.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No available COM port found. Exiting...");
                }
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //await Task.Run(() => _robotController.PollingOnce());
                    //_logger.LogDebug("輪詢成功，電壓: {Voltage}", _robotController.GetCachedVoltage());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "輪詢 Modbus 發生錯誤");
                }
                await Task.Delay(100, stoppingToken);
            }

            _logger.LogInformation("ModbusPollingService 結束");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // ASP.NET Core 會在服務停止時呼叫這裡
            Cleanup();
            await base.StopAsync(cancellationToken);
        }

        private void Cleanup()
        {
            _logger.LogInformation("ModbusPollingService 清理資源");
            _hardwareManager.Robot?.Close();
            _hardwareManager.modbusRTUService?.Close();
        }
    }
}
