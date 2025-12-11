using ChargerControlApp.Hardware;
using System.Diagnostics.Eventing.Reader;

namespace ChargerControlApp.DataAccess.CANBus.Models
{
    public class CanRouteCommandFrame
    {
        public int Index { get; set; } = 0;

        public NPB450Controller.CanbusReadCommand Command { get; set; } = NPB450Controller.CanbusReadCommand.READ_VOUT;

        public bool HasCommand { get; set; } = false; // 下達過命令

        public bool HasResponse { get; set; } = false; // 已經取得回應
    }

    public class CanRouteCommandFrameList
    {
        public int CommandIndex { get; internal set; } = 0;

        public bool IsCompletedOneTime { get; internal set; } = false;

        /// <summary>
        /// 是否讀取逾時
        /// </summary>
        public bool IsReadTimeout
        {
            get
            { 
                return (DateTime.Now.Subtract(dtReadTimeout).TotalMilliseconds > TimeoutValue_ms);
            }
        }

        public double ElapsedTime_ms
        {
            get
            {
                return DateTime.Now.Subtract(dtReadTimeout).TotalMilliseconds;
            }
        }

        public void ResetReadTimeout()
        {
            dtReadTimeout = DateTime.Now;
        }

        private DateTime dtReadTimeout = DateTime.Now; // 讀取逾時計時器

        public double TimeoutValue_ms { get; set; } = 60000;


        public CanRouteCommandFrame[] Commands = new CanRouteCommandFrame[]
        {
            new CanRouteCommandFrame()
            {
                Index = 0,
                Command = NPB450Controller.CanbusReadCommand.READ_VOUT,
                HasCommand = false,
                HasResponse = false

            },
            new CanRouteCommandFrame()
            {
                Index = 1,
                Command = NPB450Controller.CanbusReadCommand.READ_IOUT,
                HasCommand = false,
                HasResponse = false
            },
            new CanRouteCommandFrame()
            {
                Index = 2,
                Command = NPB450Controller.CanbusReadCommand.CHG_STATUS,
                HasCommand = false,
                HasResponse = false
            },
            new CanRouteCommandFrame()
            {
                Index = 3,
                Command = NPB450Controller.CanbusReadCommand.FAULT_STATUS,
                HasCommand = false,
                HasResponse = false
            }

        };

        /// <summary>
        /// 判斷是否能進行下一個命令
        /// </summary>
        /// <param name="command">下一個要執行的命令</param>
        /// <param name="isFinal">若回傳值為false，要判斷是否為已經完成所有命令</param>
        /// <returns>判斷是否要進行下一個命令:true => 可進行下一個命令; false => 尚未取得回應或已經完成所有命令 </returns>
        public bool Next(out CanRouteCommandFrame? command, out bool isFinal)
        {
            bool result = false;
            command = null;
            isFinal = false;

            //Console.WriteLine($"CanRouteCommandFrameList.Next()-CommandIndex:{CommandIndex}, HasCommand:{Commands[CommandIndex].HasCommand}, HasResponse:{Commands[CommandIndex].HasResponse}");

            try
            {
                if (Commands[CommandIndex].HasCommand)
                {
                    if (Commands[CommandIndex].HasResponse)
                    {
                        //dtReadTimeout = DateTime.Now; // 重置讀取逾時計時器

                        Commands[CommandIndex].HasCommand = false;
                        Commands[CommandIndex].HasResponse = false;
                        CommandIndex++;

                        //Console.WriteLine($"CanRouteCommandFrameList.Next()-Move to next CommandIndex:{CommandIndex}");

                        if (CommandIndex >= Commands.Length)
                        {
                            CommandIndex = 0;
                            isFinal = true;
                            IsCompletedOneTime = true;
                        }
                        else
                        { 
                            command = Commands[CommandIndex];
                            Commands[CommandIndex].HasCommand = true;
                            Commands[CommandIndex].HasResponse = false;
                            result = true;
                        }
                    }
                }
                else
                {
                    Commands[CommandIndex].HasCommand = true;
                    Commands[CommandIndex].HasResponse = false;
                    command = Commands[CommandIndex];
                    result = true;
                }
            } catch { }



            return result;
        }

        public void Reset()
        {
            for (int i = 0; i < Commands.Length; i++)
            {
                Commands[i].HasResponse = false;
                Commands[i].HasCommand = false;
                CommandIndex = 0;
            }
        }

        public bool CaptureResponse(NPB450Controller.CanbusReadCommand command)
        {
            //Console.WriteLine($"CanRouteCommandFrameList.CaptureResponse()-Command:{command}");
            bool result = true;
            try
            {
                dtReadTimeout = DateTime.Now; // 重置讀取逾時計時器

                switch (command)
                {
                    case NPB450Controller.CanbusReadCommand.READ_VOUT:
                        Commands[0].HasResponse = true;
                        //Console.WriteLine($"CanRouteCommandFrameList.CaptureResponse()-READ_VOUT-Set HasResponse true");
                        break;
                    case NPB450Controller.CanbusReadCommand.READ_IOUT:
                        Commands[1].HasResponse = true;
                        //Console.WriteLine($"CanRouteCommandFrameList.CaptureResponse()-READ_IOUT-Set HasResponse true");
                        break;
                    case NPB450Controller.CanbusReadCommand.CHG_STATUS:
                        Commands[2].HasResponse = true;
                        //Console.WriteLine($"CanRouteCommandFrameList.CaptureResponse()-CHG_STATUS-Set HasResponse true");
                        break;
                    case NPB450Controller.CanbusReadCommand.FAULT_STATUS:
                        Commands[3].HasResponse = true;
                        //Console.WriteLine($"CanRouteCommandFrameList.CaptureResponse()-FAULT_STATUS-Set HasResponse true");
                        break;
                    default:
                        result = false;
                        break;
                }
            }
            catch { result = false; }
            return result;
        }
    }
}
