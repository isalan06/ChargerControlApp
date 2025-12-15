using ChargerControlApp.DataAccess.Slot.Models;
using System.Text.Json;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public static class SlotStatePersistence
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "slot_states.json");

        public static void SaveStates(SlotInfo[] slotInfos)
        {
            var stateList = slotInfos.Select(s => new SlotStateMachineDto
            {
                Index = s.Id - 1,
                BatteryMemory = s.BatteryMemory,
                State = s.State.CurrentState.GetStateEnum()
            }).ToList();

            var json = JsonSerializer.Serialize(stateList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static void LoadStates(SlotInfo[] slotInfos)
        {
            if (!File.Exists(FilePath)) return;

            var json = File.ReadAllText(FilePath);
            var stateList = JsonSerializer.Deserialize<List<SlotStateMachineDto>>(json);

            if (stateList == null) return;

            foreach (var dto in stateList)
            {
                if (dto.Index >= 0 && dto.Index < slotInfos.Length)
                {
                    slotInfos[dto.Index].BatteryMemory = dto.BatteryMemory;
                    // To Do: Decide if we want to restore state for all slots or only those that were not "NotUsed"
                    //if (slotInfos[dto.Index].State.CurrentState.GetStateEnum() != SlotState.NotUsed)
                    //    slotInfos[dto.Index].State.TransitionToState(dto.State);
                }
            }
        }
    }
}
