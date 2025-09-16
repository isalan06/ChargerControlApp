using TAC.Hardware;

namespace ChargerControlApp.DataAccess.Slot.Models
{
    public class SlotInfo
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double ChargingProcessValue { get; set; } = 0.0; // 充電進度百分比 => 0.0 ~ 100.0
        public bool IsEnabled { get; set; } = false; // 是否啟用

        public SlotChargeState ChargeState { get; set; } = SlotChargeState.Empty; // 充電狀態


    }
}
