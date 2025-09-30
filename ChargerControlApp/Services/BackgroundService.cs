using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Modbus.Services;
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

namespace ChargerControlApp.Services
{
    public class CanBusPollingService : BackgroundService
    {
        private readonly HardwareManager _hardwareManager;
        //private readonly NPB1700Controller _npbController;
        private readonly ILogger<CanBusPollingService> _logger;
        //private readonly IServiceProvider _serviceProvider;
        private readonly ChargingStationStateMachine _chargingStationStateMachine;

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
        }
        //public CanBusPollingService(HardwareManager hardwareManager, ILogger<CanBusPollingService> logger)
        //{
        //    _hardwareManager = hardwareManager;
        //    _logger = logger;
        //}
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("背景應用模組 啟動");

            //_hardwareManager.SlotServices.State[0].TransitionTo<InitializationSlotState>();
            for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
            {
                if (i < HardwareManager.NPB450ControllerInstnaceNumber)
                {
                    _hardwareManager.Charger[i].IsUsed = true;
                }
                else
                { 
                    //_hardwareManager.SlotServices.State[i].TransitionTo<NotUsedSlotState>();
                    _hardwareManager.SlotServices.TransitionTo(i, state: SlotState.NotUsed);
                }
            }


            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    //await Task.Run(() => _hardwareManager.Charger[0].PollingOnce());

                    for (int i = 0; i < HardwareManager.NPB450ControllerInstnaceNumber; i++)
                    {
                        //_logger.LogInformation($"CanBusPollingService-DeviceID: {_hardwareManager.Charger[i].deviceCanID}-PollingOnce()");
                        //_hardwareManager.Charger[i].IsUsed = true; 
                        await Task.Run(() => _hardwareManager.Charger[i].PollingOnce());
                    }

                    // Read GPIO Inputs
                    GPIOService.ReadInputsFromHardware();

                    //_hardwareManager.Charger.PollingOnce();
                    //_logger.LogDebug("輪詢成功，電壓: {Voltage}", _hardwareManager.Charger.GetCachedVoltage());

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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "輪詢 NPB450 發生錯誤");
                }

                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("CanBusPollingService 結束");
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
                        services.AddSingleton(provider =>
                            _mainProvider.GetRequiredService<ReservedState>());
                        services.AddSingleton(provider =>
                            _mainProvider.GetRequiredService<ChargingStateClass>());
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
