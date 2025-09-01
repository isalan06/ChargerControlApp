using System.Runtime.InteropServices;

namespace ChargerControlApp.DataAccess.Motor.Services
{
    public class SingleMotorService : IDisposable
    {
        #region enum

        public enum ReadCommand
        { 
            ReadIO = 0, 
            WriteIO = 1,
        }

        #endregion

        #region structure

        /// <summary> 
        /// Sturcture of Motor IO Input High 124 (7Ch)
        /// bit 0: R-IN16 [FW-JOG-P] -> can change to [FW-JOG-X] by setting ...
        /// bit 1: R-IN17 [RV-JOG-P] -> can change to [RV-JOG-X] by setting ...
        /// bit 2: R-IN18 [FW-SPD]
        /// bit 3: R-IN19 [RV-SPD]
        /// bit 4: R-IN20 [HOME]
        /// bit 5: R-IN21 [未使用]
        /// bit 6: R-IN22 [START]
        /// bit 7: R-IN23 [SSTART]
        /// bit 8: R-IN24 [M0]
        /// bit 9: R-IN25 [M1]
        /// bit 10: R-IN26 [M2]
        /// bit 11: R-IN27 [M3]
        /// bit 12: R-IN28 [M4]
        /// bit 13: R-IN29 [M5]
        /// bit 14: R-IN30 [M6]
        /// bit 15: R-IN31 [M7]
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Motor_IO_Input_High
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;

                public bool FW_JOG_P => (Value & (1 << 0)) != 0;
                public bool RV_JOG_P => (Value & (1 << 1)) != 0;
                public bool FW_SPD => (Value & (1 << 2)) != 0;
                public bool RV_SPD => (Value & (1 << 3)) != 0;
                public bool HOME => (Value & (1 << 4)) != 0;
                public bool bit_4 => (Value & (1 << 5)) != 0;
                public bool START => (Value & (1 << 6)) != 0;
                public bool SSTART => (Value & (1 << 7)) != 0;
                public bool M0 => (Value & (1 << 8)) != 0;
                public bool M1 => (Value & (1 << 9)) != 0;
                public bool M2 => (Value & (1 << 10)) != 0;
                public bool M3 => (Value & (1 << 11)) != 0;
                public bool M4 => (Value & (1 << 12)) != 0;
                public bool M5 => (Value & (1 << 13)) != 0;
                public bool M6 => (Value & (1 << 14)) != 0;
                public bool M7 => (Value & (1 << 15)) != 0;
            }
        }

        #endregion

        #region IDisposable Support and Destructor

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~SingleMotorService()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion 


    }
}
