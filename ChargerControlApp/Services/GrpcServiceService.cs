using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using TacDynamics.Device.Protos.Charger;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Diagnostics;
using ChargerControlApp.Hardware;
using ChargerControlApp.Services;

namespace ChargerControlApp.Services
{
    public class GrpcServerService
    {
        private IHost? _host;
        private readonly int _port = 50051;

        public void Start()
        {
            Console.WriteLine("✅ GrpcServerService.Start() 被呼叫了！");

            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(_port, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });

                    webBuilder.ConfigureServices(services =>
                    {
                        //// 所有狀態註冊進來
                        //services.AddSingleton<InitializationState>();
                        //services.AddSingleton<IdleState>();
                        //services.AddSingleton<ReservedState>();
                        //services.AddSingleton<ReservationTimeoutState>();
                        //services.AddSingleton<OccupiedState>();
                        //services.AddSingleton<ChargingStateClass>();
                        //services.AddSingleton<ErrorState>();

                        // 讓 DI 自己決定實體
                        services.AddSingleton<HardwareManager>();
                        services.AddSingleton<ChargingStationStateMachine>();
                        services.AddGrpc();
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGrpcService<ChargerActionServiceImpl>();
                        });
                    });
                })
                .Build();

            _host.Start();
            Console.WriteLine($"✅ gRPC 伺服器已啟動，監聽 Port {_port}");
        }

        public void Stop()
        {
            _host?.StopAsync().Wait();
            Console.WriteLine("gRPC 伺服器已關閉");
        }
    }


    public class ChargerActionServiceImpl : ChargerActionService.ChargerActionServiceBase
    {
        private readonly HardwareManager _hardwareManager;
        private readonly ChargingStationStateMachine _chargingStationStateMachine;

        public ChargerActionServiceImpl(
            HardwareManager hardwareManager,
            ChargingStationStateMachine chargingStationStateMachine)
        {
            _hardwareManager = hardwareManager;
            _chargingStationStateMachine = chargingStationStateMachine;
        }

        public override Task<ChargerActionResponse> ChangeCurrent(ChangeCurrentRequest request, ServerCallContext context)
        {
            bool changeCurrentSuccess = false;

            Console.WriteLine($"✅ 收到 ChangeCurrent 請求: {request.MaxChargerCurrent}");

            switch (request.MaxChargerCurrent)
            {
                case MaxChargerCurrent.Current5A:
                    changeCurrentSuccess = _hardwareManager.Charger.ChangeChargingCurrent(5);
                    break;
                case MaxChargerCurrent.Current10A:
                    changeCurrentSuccess = _hardwareManager.Charger.ChangeChargingCurrent(10);
                    break;
                case MaxChargerCurrent.Current15A:
                    changeCurrentSuccess = _hardwareManager.Charger.ChangeChargingCurrent(15);
                    break;
                case MaxChargerCurrent.Current20A:
                    changeCurrentSuccess = _hardwareManager.Charger.ChangeChargingCurrent(20);
                    break;
                case MaxChargerCurrent.Current25A:
                    changeCurrentSuccess = _hardwareManager.Charger.ChangeChargingCurrent(25);
                    break;
            }

            var response = new ChargerActionResponse
            {
                Message = changeCurrentSuccess ? "Current changed successfully" : "Failed to change current",
                ResponseUuid = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0),
                NodeName = "", // 可根據需要填入裝置名
                Success = changeCurrentSuccess
            };

            return Task.FromResult(response);
        }

        public override Task<ChargerActionResponse> PostAction(PostActionRequest request, ServerCallContext context)
        {
            bool actionCommandSuccess = false;

            Console.WriteLine($"✅ 收到 PostAction 請求: {request.ChargerAction}, Device ID: {request.DeviceId}");

            switch (request.ChargerAction)
            {
                case ChargerAction.ActionReserve:
                    Console.WriteLine("Action reserve 被呼叫了！" + _chargingStationStateMachine.GetCurrentStateName());
                    _chargingStationStateMachine.TransitionTo<ReservedState>();
                    Console.WriteLine("Action reserve 被呼叫了！" + _chargingStationStateMachine.GetCurrentStateName() + " " + _chargingStationStateMachine._currentState.GetType().Name); // 這裡可以檢查狀態是否正確
                    actionCommandSuccess = true;
                    break;

                case ChargerAction.ActionCancelReservation:
                    // TODO: 加上取消預約邏輯
                    actionCommandSuccess = true;
                    break;

                case ChargerAction.ActionStartCharging:
                    _hardwareManager.Charger.StartCharging();
                    actionCommandSuccess = true;
                    break;

                case ChargerAction.ActionStopCharging:
                    _hardwareManager.Charger.StopCharging();
                    actionCommandSuccess = true;
                    break;

                case ChargerAction.ActionReset:
                    // TODO: 加上 reset 邏輯
                    actionCommandSuccess = true;
                    break;

                default:
                    Console.WriteLine("⚠️ 未指定有效的 ChargerAction");
                    break;
            }

            var response = new ChargerActionResponse
            {
                Message = actionCommandSuccess ? "Action executed successfully" : "No valid action executed",
                ResponseUuid = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0),
                NodeName = request.DeviceId ?? "Unknown",
                Success = actionCommandSuccess
            };

            return Task.FromResult(response);
        }
    }
}
