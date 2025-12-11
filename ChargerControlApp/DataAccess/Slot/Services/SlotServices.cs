using ChargerControlApp.DataAccess.Slot.Models;
using ChargerControlApp.Hardware;
using Google.Protobuf.WellKnownTypes;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Nexano.Hardware.BatterySwappingStation.Protos;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public class SlotServices
    {
        public readonly SlotInfo[] SlotInfo;
        
        public static StationState StationState { get; set; } = StationState.Unspecified; // 站台狀態
        public SlotServices(IServiceProvider serviceProvider)
        { 
            SlotInfo = serviceProvider.GetRequiredService<SlotInfo[]>();
            // 初始化每個 Slot 的狀態機
            SlotStatePersistence.LoadStates(SlotInfo); // 載入儲存的狀態
        }

        public bool IsAnySlotInErrorState
        {
            get
            { 
                bool result = false;

                for(int i=0;i<HardwareManager.NPB450ControllerInstnaceNumber;i++)
                {
                    if(SlotInfo[i].State.CurrentState.GetStateEnum() == SlotState.StateError)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
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
                case SlotState.StateError:
                case SlotState.SupplyError:
                    SlotInfo[index].ChargeState = SlotChargeState.Error;
                    break;
            }
        }

        /// <summary>
        /// 取得交換槽資訊 - 交換槽插入與拔出點位
        /// </summary>
        /// <param name="swapIn"></param>
        /// <param name="swapOut"></param>
        /// <returns></returns>
        public bool GetSwapSlotInfo(out int swapIn, out int[] swapOut)
        {
            bool result = false;
            swapIn = -1;
            List<int> batterySlots = new List<int>();
            swapOut = null;
            double voltage = 0.0;

            // ToDo: 目前簡易測試用，之後需要再修改
            // swap in - 主要是要空槽位
            // swap out - 主要是要滿槽位
            for (int i=0;i<SlotInfo.Length;i++)
            {
                if (SlotInfo[i].IsEnabled)
                {
                    if ((SlotInfo[i].ChargeState == SlotChargeState.Empty) && (SlotInfo[i].BatteryMemory == false))
                    {
                        swapIn = i+1;
                        for(int j=0;j<SlotInfo.Length;j++)
                        {
                            if (SlotInfo[j].IsEnabled)
                            {
                                if ((SlotInfo[j].ChargeState != SlotChargeState.Empty) && (SlotInfo[j].BatteryMemory == true))
                                {
                                    //swapOut = j+1;
                                    batterySlots.Add(j + 1);
                                    result = true;
                                }
                            }
                        }
                    }

                }
            }

            if (result)
            { 
                swapOut = batterySlots.ToArray();
            }

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

        public void ResetAlarm(int index)
        {
            if (index < 0 || index >= SlotInfo.Length)
            {
                Console.WriteLine($"SlotServices ResetAlarm index 超出範圍: {index}");
                return;
            }

            if((SlotInfo[index].State.CurrentState.CurrentState == SlotState.StateError) ||
                (SlotInfo[index].State.CurrentState.CurrentState == SlotState.SupplyError))
                SlotInfo[index].State.CurrentState.HandleTransition(SlotState.Initialization); // 錯誤狀態，重置為 Idle
            SlotInfo[index].StateError = false;
        }
        public void ResetAllAlarm()
        {
            for (int i = 0; i < SlotInfo.Length; i++)
            {
                ResetAlarm(i);
            }
        }

        public void ResetSlotStatus(int index)
        {
            if (index < 0 || index >= SlotInfo.Length)
            {
                Console.WriteLine($"SlotServices ResetAlarm index 超出範圍: {index}");
                return;
            }

            if (SlotInfo[index].State.CurrentState.CurrentState != SlotState.NotUsed) 
                SlotInfo[index].State.CurrentState.HandleTransition(SlotState.Initialization); // 非 NotUsed 狀態，重置為 Initialization
        }

        public void ResetAllSlotStatus()
        {
            for (int i = 0; i < SlotInfo.Length; i++)
            {
                ResetSlotStatus(i);
            }
        }
    }

    
}
