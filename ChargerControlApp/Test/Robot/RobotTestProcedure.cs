using ChargerControlApp.DataAccess.Robot.Models;
using ChargerControlApp.Hardware;

namespace ChargerControlApp.Test.Robot
{
    public class RobotTestProcedure
    {
        private readonly HardwareManager _hardwareManager;
        public bool IsRunning { get; internal set; } = false;

        public RobotTestProcedure(IServiceProvider serviceProvider)
        {
            _hardwareManager = serviceProvider.GetRequiredService<HardwareManager>();
        }


        /// <summary>   
        /// 
        /// 測試用預設動作流程1
        /// 從車輛取出電池 -> 放入 Slot 2 -> 從Slot 3 取出電池 -> 放入車輛
        /// 
        /// </summary>
        public static List<ProcedureFrame> GetDefaultTestProcedure1()
        {
            List<ProcedureFrame> procedure = new List<ProcedureFrame>()
            {
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P0",
                    Description = "Y軸回到等待點",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P0",
                    Description = "Z軸回到等待點",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 0,
                    Name = "P0",
                    Description = "旋轉軸回到Gate方向",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P1",
                    Description = "Z軸到車輛電池下方",
                    PosDataNo = 1
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P1",
                    Description = "Y軸伸進車輛電池下方",
                    PosDataNo = 1
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P2",
                    Description = "Z軸上升承接車輛電池",
                    PosDataNo = 2
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P0",
                    Description = "Y軸縮回取出車輛電池",
                    PosDataNo = 1
                },
                new PosFrame()
                {                     
                    AxisId = 0,
                    Name = "P1",
                    Description = "旋轉到S1~S4方向",
                    PosDataNo = 1
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P6",
                    Description = "Z軸上伸到Slot 2上方 - 準備放入電池",
                    PosDataNo = 6
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P2",
                    Description = "Y軸伸進Slot 2 - 放入電池",
                    PosDataNo = 2
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P5",
                    Description = "Z軸下降到Slot 2下方-放入電池",
                    PosDataNo = 5
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P0",
                    Description = "Y軸縮回等待位",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P7",
                    Description = "Z軸到達Slot 3下方 - 準備取Slot 3電池",
                    PosDataNo = 7
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P2",
                    Description = "Y軸伸進Slot 3 - 電池下方",
                    PosDataNo = 2
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P8",
                    Description = "Z軸上升承接Slot 3電池",
                    PosDataNo = 8
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P0",
                    Description = "Y軸縮回取出Slot 3電池",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P2",
                    Description = "Z軸下降到Gate上方",
                    PosDataNo = 2
                },
                new PosFrame()
                {
                    AxisId = 0,
                    Name = "P0",
                    Description = "旋轉軸選轉面向車輛 - 準備放入電池到車輛中",
                    PosDataNo = 0
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P1",
                    Description = "Y軸將電池送入到車輛中",
                    PosDataNo = 1
                },
                new PosFrame()
                {
                    AxisId = 2,
                    Name = "P1",
                    Description = "Z軸下降到Gate下方 - 將電池放入車輛中",
                    PosDataNo = 1
                },
                new PosFrame()
                {
                    AxisId = 1,
                    Name = "P0",
                    Description = "Y軸縮回等待位",
                    PosDataNo = 0
                },
            };
            return procedure;
        }

        public async Task ExecuteTestProcedure1()
        {
            if (IsRunning) return;
            IsRunning = true;
            _cts = new CancellationTokenSource();
            var procedure = GetDefaultTestProcedure1();
            try
            {
                foreach (var frame in procedure)
                {
                    if (!IsRunning) break;
                    if (frame is PosFrame posFrame)
                    {
                        await _hardwareManager.Robot.MoveToPositionAsync(posFrame.AxisId, posFrame.PosDataNo, _cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 可選：處理中斷後的清理
            }
            finally
            {
                IsRunning = false;
                _cts = null;
            }
        }

        private CancellationTokenSource _cts;

        public void StopProcedure()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _hardwareManager.Robot.AllStop();
            _cts?.Cancel();
        }

        public void TryStartTestProcedure1Background()
        {
            if (!IsRunning)
            {
                Task.Run(() => ExecuteTestProcedure1());
            }
        }
    }
}
