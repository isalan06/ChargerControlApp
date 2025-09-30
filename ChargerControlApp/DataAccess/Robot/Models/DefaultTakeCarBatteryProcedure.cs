namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class DefaultTakeCarBatteryProcedure
    {
        public static List<ProcedureFrame> ProcedureFrames = new List<ProcedureFrame>
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
                Name = "Z1",
                Description = "Z軸到車輛下方",
                PosDataNo = 1,
            },
            new SensorFrame()
            {
                Name = "Car",
                Description = "檢查車輛上是否有電池?",
                SensorName = "BatteryExistInSlot",
                CheckStatus = true,
            },
            new PosFrame()
            {
                AxisId = 1,
                Name = "Y1",
                Description = "Y軸到車輛延伸點",
                PosDataNo = 1,
            },
            new PosFrame()
            {
                AxisId = 2,
                Name = "Z2",
                Description = "Z軸到車輛上方",
                PosDataNo = 2,
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
