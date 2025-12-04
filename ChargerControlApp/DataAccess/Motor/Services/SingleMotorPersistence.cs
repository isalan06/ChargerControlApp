using ChargerControlApp.DataAccess.Motor.Models;
using System.Text.Json;
using static ChargerControlApp.DataAccess.Motor.Models.MotorInfo;

namespace ChargerControlApp.DataAccess.Motor.Services
{
    public class SingleMotorPersistence
    {
        private readonly int _index;
        private string _filePath = string.Empty;
        private string _filePathEx = string.Empty;

        public bool IsFileExist { get; internal set; } = false;
        public bool IsFileExExist { get; internal set; } = false;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public SingleMotorPersistence(int index)
        { 
            this._index = index;
            this._filePath = Path.Combine(AppContext.BaseDirectory, $"MotorPersistence_{_index}.json");
            this._filePathEx = Path.Combine(AppContext.BaseDirectory, $"MotorPersistence_{_index}_EX.json");

            this.IsFileExist = File.Exists(_filePath);
            this.IsFileExExist = File.Exists(_filePathEx);
        }

        public void Save(MotorOpDataDto[] motorOpData)
        {
            if (motorOpData == null) return;

            string dir = Path.GetDirectoryName(_filePath) ?? ".";
            Directory.CreateDirectory(dir);

            string tempPath = Path.Combine(dir, $"{Path.GetFileName(_filePath)}.tmp.{Guid.NewGuid():N}");
            string json = JsonSerializer.Serialize(motorOpData, _jsonOptions);

            // 使用 FileStream 確保能呼叫 Flush(true)
            using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(true);
            }

            // atomic replace
            try
            {
                File.Move(tempPath, _filePath, overwrite: true);
            }
            catch
            {
                // 若 Move 失敗（少見），嘗試 Replace（保留備份）
                try
                {
                    if (File.Exists(_filePath))
                        File.Replace(tempPath, _filePath, null);
                    else
                        File.Move(tempPath, _filePath);
                }
                catch
                {
                    // swallow to avoid throwing from persistence layer; 上層可加入日誌
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            this.IsFileExist = File.Exists(_filePath);

            //*** The following code is commented out to disable saving to the original file. ***//
            //var json = JsonSerializer.Serialize(motorOpData, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(_filePath, json);
        }

        public void SaveEx(MotorOpDataDto[] motorOpData)
        {
            if (motorOpData == null) return;

            string dir = Path.GetDirectoryName(_filePathEx) ?? ".";
            Directory.CreateDirectory(dir);

            string tempPath = Path.Combine(dir, $"{Path.GetFileName(_filePathEx)}.tmp.{Guid.NewGuid():N}");
            string json = JsonSerializer.Serialize(motorOpData, _jsonOptions);

            using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(true);
            }

            try
            {
                File.Move(tempPath, _filePathEx, overwrite: true);
            }
            catch
            {
                try
                {
                    if (File.Exists(_filePathEx))
                        File.Replace(tempPath, _filePathEx, null);
                    else
                        File.Move(tempPath, _filePathEx);
                }
                catch
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            this.IsFileExExist = File.Exists(_filePathEx);

        }

        public MotorOpDataDto[] Load()
        {
            if (!File.Exists(_filePath)) return Array.Empty<MotorOpDataDto>();
            try
            {
                var json = File.ReadAllText(_filePath);
                var motorOpData = JsonSerializer.Deserialize<MotorOpDataDto[]>(json, _jsonOptions);
                return motorOpData ?? Array.Empty<MotorOpDataDto>();
            }
            catch
            {
                // 若 JSON 解析失敗或內容不合理，回傳空陣列
                return Array.Empty<MotorOpDataDto>();
            }
        }

        public MotorOpDataDto[] LoadEx()
        {
            if (!File.Exists(_filePathEx)) return Array.Empty<MotorOpDataDto>();
            var json = File.ReadAllText(_filePathEx);
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
