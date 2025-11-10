namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class DefaultTest1Procedure : DefaultProcedure
    {
        public new static void Refresh()
        {
            // 這裡可以加入任何需要的初始化邏輯
            ProcedureFrames = new List<ProcedureFrame>
            {
                new TestSlotFrame()
                {
                    Name = "Test#1 Procedure-1",
                    Description = "交換程序-1: Car->Slot#1 => Slot#2->Car",
                    SlotSwapInNo = 1,
                    SlotSwapOutNo = 2,
                },
                new TestSlotFrame()
                {
                    Name = "Test#1 Procedure-2",
                    Description = "交換程序-2: Car->Slot#2 => Slot#3->Car",
                    SlotSwapInNo = 2,
                    SlotSwapOutNo = 3,
                },
                new TestSlotFrame()
                {
                    Name = "Test#1 Procedure-3",
                    Description = "交換程序-3: Car->Slot#3 => Slot#4->Car",
                    SlotSwapInNo = 3,
                    SlotSwapOutNo = 4,
                },
                new TestSlotFrame()
                { 
                    Name = "Test#1 Procedure-4",
                    Description = "交換程序-4: Car->Slot#4 => Slot#1->Car",
                    SlotSwapInNo = 4,
                    SlotSwapOutNo = 1,
                }
            };
        }
    }
}
