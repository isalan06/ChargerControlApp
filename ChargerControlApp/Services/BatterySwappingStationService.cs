using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using Grpc.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TAC.Hardware;

namespace ChargerControlApp.Services
{
    public class SwappingStationService : TAC.Hardware.SwappingStationService.SwappingStationServiceBase
    {
        private readonly ILogger<SwappingStationService> _logger;
        private readonly SlotServices _slotServices;
        private static readonly ConcurrentQueue<string> _logMessages = new();
        private readonly HardwareManager _hardwareManager;
        private readonly RobotService _robotService;
        public static IEnumerable<string> LogMessages => _logMessages;
        public SwappingStationService(ILogger<SwappingStationService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _slotServices = serviceProvider.GetRequiredService<SlotServices>();
            _hardwareManager = serviceProvider.GetRequiredService<HardwareManager>();
            _robotService = serviceProvider.GetRequiredService<RobotService>();
        }

        private void LogInformation(string message)
        {
            _logger.LogInformation(message);
            _logMessages.Enqueue(message);
            while (_logMessages.Count > 1000) // 保持隊列大小不超過1000條
            {
                _logMessages.TryDequeue(out _);
            }
        }
        public override Task<StationStatus> GetStatus(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            Random random = new Random();

            LogInformation("[gRPC] GetStatus called");

            var status = new StationStatus
            {
                State = _robotService.GetEquipmentStatus,  //SlotServices.StationState,//StationState.Idle,
                HighestSoc = 98
            };
            for(int i=0;i<HardwareManager.NPB450ControllerInstnaceNumber;i++)
            {
                var slot=_slotServices.SlotInfo[i];
                var npb450 = _hardwareManager.Charger[i];
                var slotStatus = new SlotStatus
                {
                    Name = slot.Name,
                    Soc = (int)slot.ChargingProcessValue,
                    Current = slot.ChargeState == SlotChargeState.Charging ? 15f : 0.12f,
                    Voltage = slot.ChargeState != SlotChargeState.Empty ? 54.2f - (54.2f - 48.2f) * (float)(slot.ChargingProcessValue / 100.0) : 0,
                    State = slot.ChargeState
                };
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    slotStatus.Current = (float)npb450.GetCachedCurrent();
                    slotStatus.Voltage = (float)npb450.GetCachedVoltage();
                }
                status.SlotStatuses.Add(slotStatus);
            }
            /*status.SlotStatuses.Add(new SlotStatus
            {
                Name = "Slot1",
                Soc = random.Next(0, 100),
                Current = 0.12f,
                Voltage = 54.2f,
                State = SlotChargeState.Floating
            });
            status.SlotStatuses.Add(new SlotStatus
            {
                Name = "Slot2",
                Soc = 50,
                Current = 15f,
                Voltage = 48.2f,
                State = SlotChargeState.Charging
            });
            status.SlotStatuses.Add(new SlotStatus
            {
                Name = "Slot3",
                Soc = 0,
                Current = 0,
                Voltage = 0,
                State = SlotChargeState.Empty
            });
            status.SlotStatuses.Add(new SlotStatus
            {
                Name = "Slot4",
                Soc = 0,
                Current = 0,
                Voltage = 0,
                State = SlotChargeState.Empty
            });*/
            return Task.FromResult(status);
        }

        public override Task<Google.Protobuf.WellKnownTypes.Empty> PerformAction(ActionRequest request, ServerCallContext context)
        {
            LogInformation($"[gRPC] Performing action: {request.Action}");

            // 根據請求的動作執行相應的操作
            if (request.Action == ActionType.Swap)
            {
                if (_robotService.RemoteSwappingTrigger())
                {
                    // 執行換電操作的邏輯
                    LogInformation("Executing Swap action...");
                }
                else
                {
                    LogInformation("Swap action trigger failed.");
                }

            }
            else if (request.Action == ActionType.Ems)
            {
                _robotService.StopAutoProcedure();
            }

                return Task.FromResult(new Google.Protobuf.WellKnownTypes.Empty());
        }

        public string GetCurrentStationState()
        {
            // 這裡假設 _slotServices 是 Singleton 或可靜態取得
            // 若無法靜態取得，請改用 DI 注入 SlotServices 至 Razor Page
            //return SlotServices.StationState.ToString() ?? "Unknown";
            return _robotService.GetEquipmentStatus.ToString() ?? "Unknown";
        }
    }
}
