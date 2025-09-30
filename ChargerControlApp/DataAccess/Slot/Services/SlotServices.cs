using ChargerControlApp.DataAccess.Slot.Models;
using System.Runtime.CompilerServices;
using TAC.Hardware;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public class SlotServices
    {
        public readonly SlotInfo[] SlotInfo;
        
        public StationState StationState { get; set; } = StationState.Idle; // 站台狀態
        public SlotServices(IServiceProvider serviceProvider)
        { 
            SlotInfo = serviceProvider.GetRequiredService<SlotInfo[]>();
        }

        /// <summary>
        /// 狀態轉換
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool TransitionTo(int index, SlotState state)
        { 
            bool result = false;

            if(index < 0 || index >= SlotInfo.Length)
            {
                Console.WriteLine($"SlotServices TransitionTo<T> index 超出範圍: {index}");
                return result;
            }

            result = SlotInfo[index].State._currentState.HandleTransition(state);

            return result;
        }

        public bool GetSwapSlotInfo(out int swapIn, out int swapOut)
        {
            bool result = false;

            // ToDo: 目前測試用，之後要改成讀取資料跟狀態判別點位
            swapIn = 2;
            swapOut = 3;

            result =  true;

            return result;
            
        }
    }
}
