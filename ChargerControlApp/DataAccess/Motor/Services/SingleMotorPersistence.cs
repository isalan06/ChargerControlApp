using ChargerControlApp.DataAccess.Motor.Models;
using System.Text.Json;
using static ChargerControlApp.DataAccess.Motor.Models.MotorInfo;

namespace ChargerControlApp.DataAccess.Motor.Services
{
    public class SingleMotorPersistence
    {
        private readonly int _index;
        private string _filePath = string.Empty;

        public bool IsFileExist { get; internal set; } = false;

        public SingleMotorPersistence(int index)
        { 
            this._index = index;
            this._filePath = Path.Combine(AppContext.BaseDirectory, $"MotorPersistence_{_index}.json");

            this.IsFileExist = File.Exists(_filePath);
        }

        public void Save(MotorOpDataDto[] motorOpData)
        {
            var json = JsonSerializer.Serialize(motorOpData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public MotorOpDataDto[] Load()
        {
            if (!File.Exists(_filePath)) return Array.Empty<MotorOpDataDto>();
            var json = File.ReadAllText(_filePath);
            try
            {
                var motorOpData = JsonSerializer.Deserialize<MotorOpDataDto[]>(json);
                return motorOpData ?? Array.Empty<MotorOpDataDto>();
            }
            catch (Exception ex)
            {
                // 可記錄 ex.Message 以利除錯
                return Array.Empty<MotorOpDataDto>();
            }
        }
    }
}
