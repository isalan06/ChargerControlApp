using ChargerControlApp.Hardware;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.IO;
using ChargerControlApp.DataAccess.Modbus.Services;
using System.Text.Json;

namespace ChargerControlApp.Controllers
{
    public class SystemController : Controller
    {
        public SystemController()
        {
        }

        public IActionResult Index()
        {
            var model = new Dictionary<string, string> {
                ["App"] = Assembly.GetEntryAssembly().GetName()?.Version?.ToString(),
                ["Runtime"] = Environment.Version.ToString()
            };

            // 使用與 ConfigLoader 相同的 base path（AppContext.BaseDirectory）
            string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            string candidatePath = Path.Combine(baseDir, "appsettings.json");
            string appSettingPathDisplay;

            if (System.IO.File.Exists(candidatePath))
            {
                appSettingPathDisplay = candidatePath;
            }
            else
            {
                appSettingPathDisplay = $"(未找到實體檔案) 預期位置：{candidatePath}（base: {baseDir}）";
            }

            model["AppSettingsPath"] = appSettingPathDisplay;
            ViewData["AppSettingsPath"] = appSettingPathDisplay;

            return View(model);
        }

        // 回傳 CANBus 狀態與每一台設備的 CycleCount，供前端定期查詢
        [HttpGet]
        public IActionResult CanbusStatus()
        {
            var deviceMaxCount = NPB450Controller.NPB450ControllerInstnaceMaxNumber;
            var deviceUseCount = HardwareManager.NPB450ControllerInstnaceNumber;

            uint connectedCount = 0;
            var cycles = new List<object>();

            var chargers = HttpContext.RequestServices.GetService<NPB450Controller[]>();
            if (chargers != null)
            {
                for (int i = 0; i < chargers.Length; i++)
                {
                    var c = chargers[i];
                    ulong cycle = 0;
                    if (c != null)
                    {
                        cycle = c.CycleCount;
                        if (c.IsCompletedOneTime && !c.IsReadTimeout)
                            connectedCount++;
                    }

                    cycles.Add(new
                    {
                        index = i + 1, // 顯示為 1-based index
                        cycleCount = cycle,
                        enabled = (i < HardwareManager.NPB450ControllerInstnaceNumber)
                    });
                }
            }

            return Json(new
            {
                deviceMaxCount,
                deviceUseCount,
                connectedCount,
                cycles
            });
        }

        // 回傳 Modbus 狀態，供前端定期查詢
        [HttpGet]
        public IActionResult ModbusStatus()
        {
            var modbusActFreq = ModbusRTUService.InterFrameActMilliseconds.ToString("F0");
            var modbusReadTime = ModbusRTUService.FrameReadMilliseconds.ToString("F0");

            return Json(new
            {
                modbusActFreq,
                modbusReadTime
            });
        }

        // 取得 appsettings.json 內容（raw JSON）
        [HttpGet]
        public IActionResult GetAppSettings()
        {
            string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            string candidatePath = Path.Combine(baseDir, "appsettings.json");

            if (!System.IO.File.Exists(candidatePath))
                return NotFound(new { message = "appsettings.json not found" });

            try
            {
                var content = System.IO.File.ReadAllText(candidatePath);
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to read file", detail = ex.Message });
            }
        }

        // 儲存 appsettings.json（接收 raw JSON body），會先建立備份並以暫存檔原子性取代
        [HttpPost]
        public IActionResult SaveAppSettings([FromBody] JsonElement json)
        {
            string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            string candidatePath = Path.Combine(baseDir, "appsettings.json");
            string tmpPath = candidatePath + ".tmp";
            string backupPath = candidatePath + ".bak";

            try
            {
                // 將接收到的 JsonElement 格式化（縮排），並寫入暫存檔
                var options = new JsonSerializerOptions { WriteIndented = true };
                string content = JsonSerializer.Serialize(json, options);

                System.IO.File.WriteAllText(tmpPath, content);

                // 建立備份（若存在）
                if (System.IO.File.Exists(candidatePath))
                {
                    System.IO.File.Copy(candidatePath, backupPath, overwrite: true);
                }

                // 以暫存檔取代正式檔案（原子性移動）
#if NET8_0_OR_GREATER
                System.IO.File.Move(tmpPath, candidatePath, overwrite: true);
#else
                if (System.IO.File.Exists(candidatePath))
                    System.IO.File.Delete(candidatePath);
                System.IO.File.Move(tmpPath, candidatePath);
#endif

                return Ok(new { message = "Saved" });
            }
            catch (Exception ex)
            {
                // 嘗試清理暫存檔
                try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
                return StatusCode(500, new { message = "Failed to save file", detail = ex.Message });
            }
        }
    }
}
