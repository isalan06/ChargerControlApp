using System.ComponentModel;

namespace ChargerControlApp.DataAccess.Robot.Models
{

    public class DefaultTakeSlotBatteryProcedure : DefaultProcedure
    {
        private static int _z_Input = 3;
        private static int _z_Output = 4;
        private static int _y_Output = 2;
        public static int Z_Input
        {
            get { return _z_Input; }
            set
            {
                if (value >= 3 && value <= 18)
                {
                    if ((value % 2) == 1)
                    {
                        _z_Input = value; // Down Position of slot
                        _z_Output = _z_Input + 1; // Up Position of slot
                    }

                    if (value <= 10) _y_Output = 2; else _y_Output = 3; // Y position depends on slot number

                    //var z_input_Frame = (PosFrame)ProcedureFrames[1];
                    //z_input_Frame.PosDataNo = _z_Input;
                    //var y_output_Frame = (PosFrame)ProcedureFrames[3];
                    //y_output_Frame.PosDataNo = _y_Output;
                    //var z_output_Frame = (PosFrame)ProcedureFrames[4];
                    //z_output_Frame.PosDataNo = _z_Output;
                }
            }
        }

        public static int Z_Output { get { return _z_Output; } }
        public static int Y_Output { get { return _y_Output; } }

        public static int SlotNo
        {
            get
            {
                return (_z_Input - 1) / 2;
            }
        }

        public new static void Refresh()
        {
            // 這裡可以加入任何需要的初始化邏輯
            ProcedureFrames = new List<ProcedureFrame>
            {
                new SensorFrame()
                {
                    Name = "Fork",
                    Description = "檢查Fork上是否有電池?",
                    SensorName = "BatteryExistInFork",
                    CheckStatus = false,
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "Z_Input",
                    Description = $"Z軸到 Slot#{SlotNo} 位置下方",
                    PosDataNo = Z_Input,
                },
                new SensorFrame()
                {
                    Name = "Slot",
                    Description = $"檢查 Slot#{SlotNo} 上是否有電池?",
                    SensorName = $"BatteryExistInSlot",
                    CheckStatus = true,
                    IfCheckPoint = true
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "Y_Output",
                    Description = $"Y軸到 Slot#{SlotNo} 延伸點",
                    PosDataNo = Y_Output,
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "Z_Output",
                    Description = $"Z軸到 Slot#{SlotNo} 位置上方",
                    PosDataNo = Z_Output,
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "Y0",
                    Description = "Y軸回到等待點",
                    PosDataNo = 0,
                },
                new SensorFrame()
                {
                    Name = "Fork",
                    Description = "檢查Fork上是否有電池?",
                    SensorName = "BatteryExistInFork",
                    CheckStatus = true,
                },
            };
        }

    }
}
