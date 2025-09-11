using Grpc.Core;
using TAC.Hardware;

namespace ChargerControlApp.Services
{
    public class SwappingStationService : TAC.Hardware.SwappingStationService.SwappingStationServiceBase
    {
        private readonly ILogger<SwappingStationService> _logger;
        public SwappingStationService(ILogger<SwappingStationService> logger)
        {
            _logger = logger;
        }

        public override Task<StationStatus> GetStatus(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            Random random = new Random();

            var status = new StationStatus
            {
                State = StationState.Idle,
                HighestSoc = 98
            };
            status.SlotStatuses.Add(new SlotStatus
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
            });
            return Task.FromResult(status);
        }

        public override Task<Google.Protobuf.WellKnownTypes.Empty> PerformAction(ActionRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Performing action: {request.Action}");
            return Task.FromResult(new Google.Protobuf.WellKnownTypes.Empty());
        }

    }
}
