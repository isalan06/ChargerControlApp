using ChargerControlApp.Hardware;
using ChargerControlApp.DataAccess.Robot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.IO;
using ChargerControlApp.DataAccess.Modbus.Services;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        // POST /System/SystemAction
        [HttpPost]
        public IActionResult SystemAction([FromBody] SystemActionRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Action))
                return BadRequest(new { message = "Missing Action" });

            var action = req.Action.Trim().ToLowerInvariant();
            var svc = string.IsNullOrWhiteSpace(req.ServiceName) ? "chargercontrolapp.service" : req.ServiceName.Trim();

            try
            {
                string result = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (action == "restart-service")
                    {
                        // 若需要 sudo，請在系統上配置 sudoers 無密碼執行 systemctl restart <svc>
                        result = RunProcessAndCapture("systemctl", $"restart {svc}");
                    }
                    else if (action == "reboot")
                    {
                        // 需 root 權限
                        result = RunProcessAndCapture("systemctl", "reboot");
                    }
                    else if (action == "shutdown")
                    {
                        // 需 root 權限
                        result = RunProcessAndCapture("systemctl", "poweroff");
                    }
                    else
                    {
                        return BadRequest(new { message = "Unknown action" });
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (action == "restart-service")
                    {
                        // 使用 PowerShell Restart-Service（需管理員權限）
                        result = RunProcessAndCapture("powershell", $"-Command \"Restart-Service -Name '{svc}' -Force\"");
                    }
                    else if (action == "reboot")
                    {
                        // 立刻重開機（需管理員權限）
                        result = RunProcessAndCapture("shutdown", "/r /t 0");
                    }
                    else if (action == "shutdown")
                    {
                        // 關機（需管理員權限）
                        result = RunProcessAndCapture("shutdown", "/s /t 0");
                    }
                    else
                    {
                        return BadRequest(new { message = "Unknown action" });
                    }
                }
                else
                {
                    return StatusCode(501, new { message = "Unsupported OS" });
                }

                return Ok(new { message = "Command started", detail = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to execute", detail = ex.Message });
            }
        }

        // --- 新增：保存 SwapLog 呼叫 RobotService.SaveSlotInfoBeforeSwap ---
        public class SaveSwapRequest
        {
            public int SwapIn { get; set; }
            public int SwapOut { get; set; }
        }

        [HttpPost]
        public IActionResult SaveSwapLog([FromBody] SaveSwapRequest req)
        {
            if (req == null) return BadRequest(new { message = "Missing body" });

            var robot = HttpContext.RequestServices.GetService<RobotService>();
            if (robot == null) return StatusCode(500, new { message = "RobotService not available" });

            try
            {
                robot.SaveSlotInfoBeforeSwap(req.SwapIn, req.SwapOut);
                return Ok(new { message = "Saved" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Save failed", detail = ex.Message });
            }
        }

        // 列出 SwapLog 資料夾下檔案，支援 fromDate, toDate, keyword 過濾
        [HttpGet]
        public IActionResult ListSwapLogs(string fromDate = null, string toDate = null, string keyword = null)
        {
            try
            {
                string logDir = Path.Combine(AppContext.BaseDirectory, "SwapLog");
                if (!Directory.Exists(logDir))
                    return Json(new object[0]);

                DateTime? from = null, to = null;
                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var f)) from = f.Date;
                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var t)) to = t.Date.AddDays(1).AddTicks(-1);

                var files = Directory.GetFiles(logDir, "*.log")
                                     .Select(f => new FileInfo(f))
                                     .Where(fi =>
                                     {
                                         if (from.HasValue && fi.LastWriteTime < from.Value) return false;
                                         if (to.HasValue && fi.LastWriteTime > to.Value) return false;
                                         if (!string.IsNullOrEmpty(keyword) && !fi.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !fi.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false;
                                         return true;
                                     })
                                     .Select(fi => new {
                                         name = fi.Name,
                                         length = fi.Length,
                                         lastWrite = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                     })
                                     .OrderByDescending(f => f.lastWrite)
                                     .ToArray();
                return Json(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "List failed", detail = ex.Message });
            }
        }

        // 讀取指定檔案內容（從 SwapLog 資料夾），避免路徑穿越
        [HttpGet]
        public IActionResult ReadSwapLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest(new { message = "Missing fileName" });

            // 檢查是否包含路徑分隔符，避免路徑穿越
            if (fileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
                return BadRequest(new { message = "Invalid fileName" });

            try
            {
                string logDir = Path.Combine(AppContext.BaseDirectory, "SwapLog");
                string fullPath = Path.Combine(logDir, fileName);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound(new { message = "File not found" });

                var content = System.IO.File.ReadAllText(fullPath);
                return Content(content, "text/plain");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Read failed", detail = ex.Message });
            }
        }

        // 下載指定 SwapLog（attachment），避免路徑穿越
        [HttpGet]
        public IActionResult DownloadSwapLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest(new { message = "Missing fileName" });

            if (fileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
                return BadRequest(new { message = "Invalid fileName" });

            try
            {
                string logDir = Path.Combine(AppContext.BaseDirectory, "SwapLog");
                string fullPath = Path.Combine(logDir, fileName);

                // 安全檢查：確保 fullPath 在 logDir 中
                var fullPathResolved = Path.GetFullPath(fullPath);
                var logDirResolved = Path.GetFullPath(logDir);
                if (!fullPathResolved.StartsWith(logDirResolved, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Invalid file path" });

                if (!System.IO.File.Exists(fullPathResolved))
                    return NotFound(new { message = "File not found" });

                var bytes = System.IO.File.ReadAllBytes(fullPathResolved);
                return File(bytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Download failed", detail = ex.Message });
            }
        }

        // System action DTO
        public class SystemActionRequest
        {
            public string Action { get; set; } = ""; // "restart-service" | "reboot" | "shutdown"
            public string ServiceName { get; set; } = "chargercontrolapp.service"; // optional
        }

        private static string RunProcessAndCapture(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) throw new InvalidOperationException("Process failed to start");
            // 等待短時間以便捕捉輸出；某些重啟命令會立即終止進程或無輸出
            proc.WaitForExit(5000);
            string outp = proc.StandardOutput?.ReadToEnd() ?? "";
            string err = proc.StandardError?.ReadToEnd() ?? "";
            return $"ExitCode={proc.ExitCode}; StdOut={outp}; StdErr={err}";
        }
    }
}
