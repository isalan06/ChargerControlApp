namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class TestSlotFrame: ProcedureFrame
    {
        public string Name { get; set; } = string.Empty; // 名稱

        public string Description { get; set; } = string.Empty; // 描述

        public int SlotSwapInNo { get; set; } = 1; // Slot 交換進來編號; 由1開始

        public int SlotSwapOutNo { get; set; } = 2; // Slot 交換出去編號; 由1開始
    }
}
