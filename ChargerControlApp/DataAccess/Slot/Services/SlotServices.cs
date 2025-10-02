using ChargerControlApp.DataAccess.Slot.Models;
using Google.Protobuf.WellKnownTypes;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
            // 初始化每個 Slot 的狀態機
            SlotStatePersistence.LoadStates(SlotInfo); // 載入儲存的狀態
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

            result = SlotInfo[index].State.CurrentState.HandleTransition(state); // 呼叫當前狀態的 HandleTransition 方法

            // 轉換成功後，可以在這裡執行一些後續操作
            if (result)
            {
                TransferToSlotChargeState(index); // 狀態轉換後，同步更新充電狀態
                SlotStatePersistence.SaveStates(SlotInfo); // 儲存狀態
            }

            return result;
        }

        public void TransferToSlotChargeState(int index)
        { 
            var temp = SlotInfo[index].State.CurrentState.GetStateEnum();
            switch (SlotInfo[index].State.CurrentState.GetStateEnum())
            {
                case SlotState.Initialization:
                case SlotState.Empty:
                    SlotInfo[index].ChargeState = SlotChargeState.Empty;
                    break;
                default:
                case SlotState.NotUsed:
                case SlotState.SupplyError:
                case SlotState.StateError:
                    SlotInfo[index].ChargeState = SlotChargeState.Unspecified;
                    break;
                case SlotState.Idle:
                case SlotState.Charging:
                case SlotState.StopCharge:
                    SlotInfo[index].ChargeState = SlotChargeState.Charging;
                    break;
                case SlotState.Floating:
                    SlotInfo[index].ChargeState = SlotChargeState.Floating;
                    break;
            }
        }

        /// <summary>
        /// 取得交換槽資訊 - 交換槽插入與拔出點位
        /// </summary>
        /// <param name="swapIn"></param>
        /// <param name="swapOut"></param>
        /// <returns></returns>
        public bool GetSwapSlotInfo(out int swapIn, out int swapOut)
        {
            bool result = false;

            // ToDo: 目前測試用，之後要改成讀取資料跟狀態判別點位
            swapIn = 2;
            swapOut = 3;

            result =  true;

            return result;
            
        }

        /// <summary>
        /// 設定電池記憶
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetBatteryMemory(int index, bool value, bool save = true)
        {
            if (index < 0 || index >= SlotInfo.Length)
            {
                Console.WriteLine($"SlotServices SetBatteryMemory index 超出範圍: {index}");
                return;
            }
            SlotInfo[index].BatteryMemory = value;
            if (save)
                SlotStatePersistence.SaveStates(SlotInfo); // 儲存狀態
        }

        public void SwapBatteryMemory(int index, bool save = true)
        {
            if (index < 0 || index >= SlotInfo.Length)
            {
                Console.WriteLine($"SlotServices SetBatteryMemory index 超出範圍: {index}");
                return;
            }
            SlotInfo[index].BatteryMemory = !SlotInfo[index].BatteryMemory;
            if (save)
                SlotStatePersistence.SaveStates(SlotInfo); // 儲存狀態
        }
    }

    
}
