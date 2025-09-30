namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorAlarmList
    {
        /// <summary>
        /// Retrieves the description of an alarm based on its code.
        /// </summary>
        /// <remarks>The method checks if the provided alarm code exists in the predefined alarm list. If
        /// the code is not found, it returns a default message indicating that the alarm is unknown.</remarks>
        /// <param name="code">The unique integer code representing the alarm.</param>
        /// <returns>A string containing the description of the alarm if the code exists in the alarm list; otherwise, a string
        /// indicating an unknown alarm with the hexadecimal representation of the code.</returns>
        public static string GetAlarmDescription(int code)
        {
            if (AlarmList.ContainsKey(code))
            {
                return AlarmList[code];
            }
            else
            {
                return $"未知異常: 0x{code:X}";
            }
        }

        /// <summary>
        /// alarm code 對應的描述
        /// </summary>
        public static Dictionary<int, string> AlarmList = new Dictionary<int, string>()
        {
            { 0x0, "無異常" },
            { 0x10, "位置偏差過大" },
            { 0x20, "電流過大" },
            { 0x21, "主回路過熱" },
            { 0x22, "過電壓" },
            { 0x25, "電壓不足" },
            { 0x26, "馬達過熱" },
            { 0x28, "編碼器異常" },
            { 0x29, "內部回路異常" },
            { 0x2A, "編碼器通訊異常" },
            { 0x2D, "馬達連接異常" },
            { 0x30, "過負載" },
            { 0x31, "超速" },
            { 0x41, "EEPROM異常" },
            { 0x42, "初期時編碼器異常" },
            { 0x44, "編碼器EEPROM異常" },
            { 0x45, "馬達組合異常" },
            { 0x4A, "原點復歸未完成" },
            { 0x50, "電磁煞車電流過大" },
            { 0x53, "HWTO輸入回路異常" },
            { 0x55, "電磁煞車連接異常" },
            { 0x60, "+-LS同時輸入" },
            { 0x61, "+-LS反方向連接" },
            { 0x62, "原點復歸運轉異常" },
            { 0x63, "未檢測出 HOMES" },
            { 0x64, "Z, SLIT 信號異常" },
            { 0x66, "硬體超程" },
            { 0x67, "軟體超程" },
            { 0x68, "HWTO 輸入檢測" },
            { 0x6A, "原點復歸追加運轉異常" },
            { 0x6E, "用戶 Alarm" },
            { 0x70, "運轉資料異常" },
            { 0x71, "單位設定異常" },
            { 0x81, "網路匯流排異常" },
            { 0x84, "RS-485 通訊異常" },
            { 0x85, "RS-485 通訊超時" },
            { 0x8C, "設定範圍外" },
            { 0xF0, "CPU 異常" },
            { 0xF3, "CPU 過負載" }
        };
    }
}
