using ChargerControlApp.DataAccess.Slot.Models;
using System.Runtime.CompilerServices;
using TAC.Hardware;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public class SlotServices
    {
        public readonly SlotInfo[] SlotInfo;
        private readonly SlotStateMachine[] _state;
        public StationState StationState { get; set; } = StationState.Idle; // 站台狀態
        public SlotServices(IServiceProvider serviceProvider)
        { 
            _state = serviceProvider.GetRequiredService<SlotStateMachine[]>();
            SlotInfo = serviceProvider.GetRequiredService<SlotInfo[]>();
        }
    }
}
