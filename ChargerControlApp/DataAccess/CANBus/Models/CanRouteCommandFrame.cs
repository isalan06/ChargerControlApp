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
        public static int CommandIndex { get; internal set; } = 0;

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

        private DateTime dtReadTimeout = DateTime.Now; // 讀取逾時計時器

        public double TimeoutValue_ms { get; set; } = 60000;
        

        public List<CanRouteCommandFrame> Commands = new List<CanRouteCommandFrame>()
        {
            new CanRouteCommandFrame()
            {
                Index = 0,
                Command = NPB450Controller.CanbusReadCommand.READ_VOUT
            },
            new CanRouteCommandFrame()
            { 
                Index = 1,
                Command = NPB450Controller.CanbusReadCommand.READ_IOUT
            },
            new CanRouteCommandFrame()
            { 
                Index = 2,
                Command = NPB450Controller.CanbusReadCommand.CHG_STATUS
            },
            new CanRouteCommandFrame()
            { 
                Index = 3,
                Command = NPB450Controller.CanbusReadCommand.FAULT_STATUS
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

            try
            {
                if (Commands[CommandIndex].HasCommand)
                {
                    if (Commands[CommandIndex].HasResponse)
                    {
                        dtReadTimeout = DateTime.Now; // 重置讀取逾時計時器

                        Commands[CommandIndex].HasCommand = false;
                        Commands[CommandIndex].HasResponse = false;

                        if (++CommandIndex > Commands.Count)
                        {
                            CommandIndex = 0;
                            isFinal = true;
                            IsCompletedOneTime = true;
                        }
                        else
                        { 
                            command = Commands[CommandIndex];
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
            for (int i = 0; i < Commands.Count; i++)
            {
                Commands[i].HasResponse = false;
                Commands[i].HasCommand = false;
                CommandIndex = 0;
            }
        }

        public bool CaptureResponse(NPB450Controller.CanbusReadCommand command)
        {
            bool result = true;
            try
            {
                switch (command)
                {
                    case NPB450Controller.CanbusReadCommand.READ_VOUT:
                        Commands[0].HasResponse = true;
                        break;
                    case NPB450Controller.CanbusReadCommand.READ_IOUT:
                        Commands[1].HasResponse = true;
                        break;
                    case NPB450Controller.CanbusReadCommand.CHG_STATUS:
                        Commands[2].HasResponse = true;
                        break;
                    case NPB450Controller.CanbusReadCommand.FAULT_STATUS:
                        Commands[3].HasResponse = true;
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
