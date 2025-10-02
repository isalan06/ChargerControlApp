using ChargerControlApp.DataAccess.Slot.Services;
using TacDynamics.Kernel.DeviceService.Protos;

namespace ChargerControlApp.DataAccess.Slot.Models
{
    public class SlotStateMachineDto
    {
        public int Index { get; set; }

        public bool BatteryMemory { get; set; }
        public SlotState State { get; set; }
    }
}
