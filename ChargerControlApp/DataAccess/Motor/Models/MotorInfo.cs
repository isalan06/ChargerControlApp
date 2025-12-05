using System.Collections;
using System.Runtime.InteropServices;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorInfo
    {
        public int Id { get; set; } = 0;

        #region Read IO

        public Motor_IO_Input_High IO_Input_High;
        public Motor_IO_Input_Low IO_Input_Low;
        public Motor_IO_Output_High IO_Output_High;
        public Motor_IO_Output_Low IO_Output_Low;
        public Motor_Jog_Home_Setting Jog_Home_Setting;
        public int ErrorCode { get; set; } = 0;
        public string ErrorMessage
        {
            get
            {
                return MotorAlarmList.GetAlarmDescription(ErrorCode);
            }
        }

        public int CurrentPosNo
        {
            get
            {
                // M0..M7 對應到 IO_Input_High.Bits.Value 的 bit 8..15
                // 右移 8 位再取低 8 位即可得到 0..255 的整數值
                return (IO_Input_High.Bits.Value >> 8) & 0xFF;
            }
        }

        #endregion

        #region Read Pos and Vel

        public int Pos_Target { get; set; } = 0; // unit: step
        public int Pos_Command { get; set; } = 0; // unit: step
        public int Pos_Actual { get; set; } = 0; // unit: step
        public int Vel_Target { get; set; } = 0; // unit: r/min    
        public int Vel_Command { get; set; } = 0; // unit: r/min
        public int Vel_Actual { get; set; } = 0; // unit: r/min
        public int ErrorComm { get; set; } = 0;

        #endregion

        #region Read Operation Data

        public int OpData_IdSelect { get; set; } = 0;
        public int OpData_IdOp { get; set; } = 0;
        public int OpData_Pos_Command { get; set; } = 0; // unit: step
        public int OpData_VelR_Command { get; set; } = 0;  // unit: r/min
        public int OpData_Vel_Command { get; set; } = 0;  // unit: step/sec
        public int OpData_Pos_Actual { get; set; } = 0;  // unit: step
        public int OpData_VelR_Actual { get; set; } = 0;  // unit: r/min
        public int OpData_Vel_Actual { get; set; } = 0;  // unit: step/sec

        public double OpData_Trq_Monitor { get; set; } = 0;  // unit: 0.1 %

        public double OpData_Load_Monitor { get; set; } = 0; // unit: 0.1 %

        public int CurrentDataNo { get; set; } = 0;

        public MotorOpDataDto[] OpDataArray { get; set; } = new MotorOpDataDto[20];
        public MotorOpDataDto[] OpDataExArray { get; set; } = new MotorOpDataDto[48];

        #endregion

        #region Read Jog Setting

        public int JogMode { get; set; } = 2;  // 0: Low Ppeed; 1; High Speed; 2: Pitch

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

                public bool FW_JOG_P { get { return (Value & (1 << 0)) != 0; } set { ushort mask = 0xFFFF - (1 << 0); Value &= mask; if (value) Value += (1 << 0); } }
                public bool RV_JOG_P { get { return (Value & (1 << 1)) != 0; } set { ushort mask = 0xFFFF - (1 << 1); Value &= mask; if (value) Value += (1 << 1); } }
                public bool FW_SPD { get { return (Value & (1 << 2)) != 0; } set { ushort mask = 0xFFFF - (1 << 2); Value &= mask; if (value) Value += (1 << 2); } }
                public bool RV_SPD { get { return (Value & (1 << 3)) != 0; } set { ushort mask = 0xFFFF - (1 << 3); Value &= mask; if (value) Value += (1 << 3); } }
                public bool HOME { get { return (Value & (1 << 4)) != 0; } set { ushort mask = 0xFFFF - (1 << 4); Value &= mask; if (value) Value += (1 << 4); } }
                public bool bit_4 { get { return (Value & (1 << 5)) != 0; } set { ushort mask = 0xFFFF - (1 << 5); Value &= mask; if (value) Value += (1 << 5); } }
                public bool START { get { return (Value & (1 << 6)) != 0; } set { ushort mask = 0xFFFF - (1 << 6); Value &= mask; if (value) Value += (1 << 6); } }
                public bool SSTART { get { return (Value & (1 << 7)) != 0; } set { ushort mask = 0xFFFF - (1 << 7); Value &= mask; if (value) Value += (1 << 7); } }
                public bool M0 { get { return (Value & (1 << 8)) != 0; } set { ushort mask = 0xFFFF - (1 << 8); Value &= mask; if (value) Value += (1 << 8); } }
                public bool M1 { get { return (Value & (1 << 9)) != 0; } set { ushort mask = 0xFFFF - (1 << 9); Value &= mask; if (value) Value += (1 << 9); } }
                public bool M2 { get { return (Value & (1 << 10)) != 0; } set { ushort mask = 0xFFFF - (1 << 10); Value &= mask; if (value) Value += (1 << 10); } }
                public bool M3 { get { return (Value & (1 << 11)) != 0; } set { ushort mask = 0xFFFF - (1 << 11); Value &= mask; if (value) Value += (1 << 11); } }
                public bool M4 { get { return (Value & (1 << 12)) != 0; } set { ushort mask = 0xFFFF - (1 << 12); Value &= mask; if (value) Value += (1 << 12); } }
                public bool M5 { get { return (Value & (1 << 13)) != 0; } set { ushort mask = 0xFFFF - (1 << 13); Value &= mask; if (value) Value += (1 << 13); } }
                public bool M6 { get { return (Value & (1 << 14)) != 0; } set { ushort mask = 0xFFFF - (1 << 14); Value &= mask; if (value) Value += (1 << 14); } }
                public bool M7 { get { return (Value & (1 << 15)) != 0; } set { ushort mask = 0xFFFF - (1 << 15); Value &= mask; if (value) Value += (1 << 15); } }
            }
        }

        /// <summary> 
        /// Sturcture of Motor IO Input Low 125 (7Dh)
        /// bit 0: R-IN0 [S-ON]
        /// bit 1: R-IN1 [PLOOP-MODE]
        /// bit 2: R-IN2 [TRQ-LMT]
        /// bit 3: R-IN3 [CLR]
        /// bit 4: R-IN4 [QSTOP]
        /// bit 5: R-IN5 [STOP]
        /// bit 6: R-IN6 [FREE]
        /// bit 7: R-IN7 [ALM-RST]
        /// bit 8: R-IN8 [D-SEL0]
        /// bit 9: R-IN9 [D-SEL1]
        /// bit 10: R-IN10 [D-SEL2]
        /// bit 11: R-IN11 [D-SEL3]
        /// bit 12: R-IN12 [D-SEL4]
        /// bit 13: R-IN13 [D-SEL5]
        /// bit 14: R-IN14 [D-SEL6]
        /// bit 15: R-IN15 [D-SEL7]
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Motor_IO_Input_Low
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;

                public bool S_ON { get { return (Value & (1 << 0)) != 0; } set { ushort mask = 0xFFFF - (1 << 0); Value &= mask; if (value) Value += (1 << 0); } }
                public bool PLOOP_MODE { get { return (Value & (1 << 1)) != 0; } set { ushort mask = 0xFFFF - (1 << 1); Value &= mask; if (value) Value += (1 << 1); } }
                public bool TRQ_LMT { get { return (Value & (1 << 2)) != 0; } set { ushort mask = 0xFFFF - (1 << 2); Value &= mask; if (value) Value += (1 << 2); } }
                public bool CLR { get { return (Value & (1 << 3)) != 0; } set { ushort mask = 0xFFFF - (1 << 3); Value &= mask; if (value) Value += (1 << 3); } }
                public bool QSTOP { get { return (Value & (1 << 4)) != 0; } set { ushort mask = 0xFFFF - (1 << 4); Value &= mask; if (value) Value += (1 << 4); } }
                public bool STOP { get { return (Value & (1 << 5)) != 0; } set { ushort mask = 0xFFFF - (1 << 5); Value &= mask; if (value) Value += (1 << 5); } }
                public bool FREE { get { return (Value & (1 << 6)) != 0; } set { ushort mask = 0xFFFF - (1 << 6); Value &= mask; if (value) Value += (1 << 6); } }
                public bool ALM_RST { get { return (Value & (1 << 7)) != 0; } set { ushort mask = 0xFFFF - (1 << 7); Value &= mask; if (value) Value += (1 << 7); } }
                public bool D_SEL0 { get { return (Value & (1 << 8)) != 0; } set { ushort mask = 0xFFFF - (1 << 8); Value &= mask; if (value) Value += (1 << 8); } }
                public bool D_SEL1 { get { return (Value & (1 << 9)) != 0; } set { ushort mask = 0xFFFF - (1 << 9); Value &= mask; if (value) Value += (1 << 9); } }
                public bool D_SEL2 { get { return (Value & (1 << 10)) != 0; } set { ushort mask = 0xFFFF - (1 << 10); Value &= mask; if (value) Value += (1 << 10); } }
                public bool D_SEL3 { get { return (Value & (1 << 11)) != 0; } set { ushort mask = 0xFFFF - (1 << 11); Value &= mask; if (value) Value += (1 << 11); } }
                public bool D_SEL4 { get { return (Value & (1 << 12)) != 0; } set { ushort mask = 0xFFFF - (1 << 12); Value &= mask; if (value) Value += (1 << 12); } }
                public bool D_SEL5 { get { return (Value & (1 << 13)) != 0; } set { ushort mask = 0xFFFF - (1 << 13); Value &= mask; if (value) Value += (1 << 13); } }
                public bool D_SEL6 { get { return (Value & (1 << 14)) != 0; } set { ushort mask = 0xFFFF - (1 << 14); Value &= mask; if (value) Value += (1 << 14); } }
                public bool D_SEL7 { get { return (Value & (1 << 15)) != 0; } set { ushort mask = 0xFFFF - (1 << 15); Value &= mask; if (value) Value += (1 << 15); } }
            }
        }

        /// <summary> 
        /// Sturcture of Motor IO Output High 126 (7Eh)
        /// bit 0: R-OUT16 [INFO]
        /// bit 1: R-OUT17 [INFO-MNT-G]
        /// bit 2: R-OUT18 [INFO-DRVTMP]
        /// bit 3: R-OUT19 [INFO-MTRTMP]
        /// bit 4: R-OUT20 [INFO-TRQ]
        /// bit 5: R-OUT21 [INFO-WATT]
        /// bit 6: R-OUT22 [INFO-VOLT-H]
        /// bit 7: R-OUT23 [INFO-VOLT-L]
        /// bit 8: R-OUT24 [INFO-START-G]
        /// bit 9: R-OUT25 [INFO-USRIO-G]
        /// bit 10: R-OUT26 [CONST-OFF] -> R0_R
        /// bit 11: R-OUT27 [CONST-OFF] -> R1_R
        /// bit 12: R-OUT28 [CONST-OFF] -> HOME-END
        /// bit 13: R-OUT29 [CONST-OFF]
        /// bit 14: R-OUT30 [USR-OUT0]
        /// bit 15: R-OUT31 [USR-OUT1]
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Motor_IO_Output_High
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;

                public bool INFO => (Value & (1 << 0)) != 0;
                public bool INFO_MNT_G => (Value & (1 << 1)) != 0;
                public bool INFO_DRVTMP => (Value & (1 << 2)) != 0;
                public bool INFO_MTRTMP => (Value & (1 << 3)) != 0;
                public bool INFO_TRQ => (Value & (1 << 4)) != 0;
                public bool INFO_WATT => (Value & (1 << 5)) != 0;
                public bool INFO_VOLT_H => (Value & (1 << 6)) != 0;
                public bool INFO_VOLT_L => (Value & (1 << 7)) != 0;
                public bool INFO_START_G => (Value & (1 << 8)) != 0;
                public bool INFO_USRIO_G => (Value & (1 << 9)) != 0;
                public bool R0_R => (Value & (1 << 10)) != 0;
                public bool R1_R => (Value & (1 << 11)) != 0;
                public bool HOME_END => (Value & (1 << 12)) != 0;
                public bool CONST_OFF4 => (Value & (1 << 13)) != 0;
                public bool USR_OUT0 => (Value & (1 << 14)) != 0;
                public bool USR_OUT1 => (Value & (1 << 15)) != 0;
            }
        }


        /// <summary> 
        /// Sturcture of Motor IO Output Low 127 (7Fh)
        /// bit 0: R-OUT0 [SON-MON]
        /// bit 1: R-OUT1 [PLOOP-MON]
        /// bit 2: R-OUT2 [TRQ-LMTD]
        /// bit 3: R-OUT3 [RDY-DD-OPE]
        /// bit 4: R-OUT4 [ABSPEN]
        /// bit 5: R-OUT5 [STOP_R]
        /// bit 6: R-OUT6 [FREE_R]
        /// bit 7: R-OUT7 [ALM-A]
        /// bit 8: R-OUT8 [SYS-BSY]
        /// bit 9: R-OUT9 [IN-POS]
        /// bit 10: R-OUT10 [RDY-HOME-OPE]
        /// bit 11: R-OUT11 [RDY-FWRV-OPE]
        /// bit 12: R-OUT12 [RDY-SD-OPE]
        /// bit 13: R-OUT13 [MOVE]
        /// bit 14: R-OUT14 [VA]
        /// bit 15: R-OUT15 [TLC]
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Motor_IO_Output_Low
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;

                public bool SON_MON => (Value & (1 << 0)) != 0;
                public bool PLOOP_MON => (Value & (1 << 1)) != 0;
                public bool TRQ_LMTD => (Value & (1 << 2)) != 0;
                public bool RDY_DD_OPE => (Value & (1 << 3)) != 0;
                public bool ABSPEN => (Value & (1 << 4)) != 0;
                public bool STOP_R => (Value & (1 << 5)) != 0;
                public bool FREE_R => (Value & (1 << 6)) != 0;
                public bool ALM_A => (Value & (1 << 7)) != 0;
                public bool SYS_BSY => (Value & (1 << 8)) != 0;
                public bool IN_POS => (Value & (1 << 9)) != 0;
                public bool RDY_HOME_OPE => (Value & (1 << 10)) != 0;
                public bool RDY_FWRV_OPE => (Value & (1 << 11)) != 0;
                public bool RDY_SD_OPE => (Value & (1 << 12)) != 0;
                public bool MOVE => (Value & (1 << 13)) != 0;
                public bool VA => (Value & (1 << 14)) != 0;
                public bool TLC => (Value & (1 << 15)) != 0;
            }
        }

        #region Jog And Home Setting

        public struct Motor_Jog_Home_Setting
        {

            public int JogDistance { get; set; } = 1; // unit: step
            public int JogSpeed { get; set; } = 100; // unit: r/min
            public int JogAccDec { get; set; } = 1000; // unit: ms
            public int JogStartVel { get; set; } = 0; // unit: r/min
            public int JogHighSpeed { get; set; } = 500; // unit: r/min
            public int JogHomeOpCommandSTimeConst { get; set; } = 1; // unit: ms
            public int JogHomeOpTorqueLmt { get; set; } = 10000; // unit: 0.1 %
            public int HomeType { get; set; } = 1; // 0: 2檢知器; 1: 3檢知器; 2: 單一方向旋轉; 3: 推壓; 
            public int HomeDir { get; set; } = 1; // 0: 反轉(-側); 1: 正轉(+側)
            public int HomeAccDec { get; set; } = 1000; // unit: ms
            public int HomeStartVel { get; set; } = 30; // unit: r/min
            public int HomeSpeed { get; set; } = 60; // unit: r/min
            public int HomeDetectVel { get; set; } = 30; // unit: r/min
            public int HomeSLITDetect { get; set; } = 0; // 0: 無效; 1: 有效
            public int HomeZSGDetect { get; set; } = 0; // 0: 無效; 2: ZSG
            public int HomeOffset { get; set; } = 0; // unit: step


            public Motor_Jog_Home_Setting() { }

            public void Set(ushort[] setValue)
            {
                if (setValue == null || setValue.Length != 32)
                    throw new ArgumentException("setValue 必須為 32 個 ushort 元素的陣列");

                // 合併高低位元，正確處理負數
                JogDistance = CombineUShortToInt(setValue[0], setValue[1]);
                JogSpeed = CombineUShortToInt(setValue[2], setValue[3]);
                JogAccDec = CombineUShortToInt(setValue[4], setValue[5]);
                JogStartVel = CombineUShortToInt(setValue[6], setValue[7]);
                JogHighSpeed = CombineUShortToInt(setValue[8], setValue[9]);
                JogHomeOpCommandSTimeConst = CombineUShortToInt(setValue[10], setValue[11]);
                JogHomeOpTorqueLmt = CombineUShortToInt(setValue[12], setValue[13]);
                HomeType = CombineUShortToInt(setValue[14], setValue[15]);
                HomeDir = CombineUShortToInt(setValue[16], setValue[17]);
                HomeAccDec = CombineUShortToInt(setValue[18], setValue[19]);
                HomeStartVel = CombineUShortToInt(setValue[20], setValue[21]);
                HomeSpeed = CombineUShortToInt(setValue[22], setValue[23]);
                HomeDetectVel = CombineUShortToInt(setValue[24], setValue[25]);
                HomeSLITDetect = CombineUShortToInt(setValue[26], setValue[27]);
                HomeZSGDetect = CombineUShortToInt(setValue[28], setValue[29]);
                HomeOffset = CombineUShortToInt(setValue[30], setValue[31]);
            }

            // 工具方法：合併高低 ushort 為 int，支援負數
            private static int CombineUShortToInt(ushort high, ushort low)
            {
                uint combined = ((uint)high << 16) | (uint)low;
                return unchecked((int)combined);
            }

            public ushort[] Get()
            {
                ushort[] result = new ushort[32];

                // 依序拆解每個 int 屬性為高低兩個 ushort
                result[0] = (ushort)((JogDistance >> 16) & 0xFFFF); // 高位
                result[1] = (ushort)(JogDistance & 0xFFFF);         // 低位
                result[2] = (ushort)((JogSpeed >> 16) & 0xFFFF);
                result[3] = (ushort)(JogSpeed & 0xFFFF);
                result[4] = (ushort)((JogAccDec >> 16) & 0xFFFF);
                result[5] = (ushort)(JogAccDec & 0xFFFF);
                result[6] = (ushort)((JogStartVel >> 16) & 0xFFFF);
                result[7] = (ushort)(JogStartVel & 0xFFFF);
                result[8] = (ushort)((JogHighSpeed >> 16) & 0xFFFF);
                result[9] = (ushort)(JogHighSpeed & 0xFFFF);
                result[10] = (ushort)((JogHomeOpCommandSTimeConst >> 16) & 0xFFFF);
                result[11] = (ushort)(JogHomeOpCommandSTimeConst & 0xFFFF);
                result[12] = (ushort)((JogHomeOpTorqueLmt >> 16) & 0xFFFF);
                result[13] = (ushort)(JogHomeOpTorqueLmt & 0xFFFF);
                result[14] = (ushort)((HomeType >> 16) & 0xFFFF);
                result[15] = (ushort)(HomeType & 0xFFFF);
                result[16] = (ushort)((HomeDir >> 16) & 0xFFFF);
                result[17] = (ushort)(HomeDir & 0xFFFF);
                result[18] = (ushort)((HomeAccDec >> 16) & 0xFFFF);
                result[19] = (ushort)(HomeAccDec & 0xFFFF);
                result[20] = (ushort)((HomeStartVel >> 16) & 0xFFFF);
                result[21] = (ushort)(HomeStartVel & 0xFFFF);
                result[22] = (ushort)((HomeSpeed >> 16) & 0xFFFF);
                result[23] = (ushort)(HomeSpeed & 0xFFFF);
                result[24] = (ushort)((HomeDetectVel >> 16) & 0xFFFF);
                result[25] = (ushort)(HomeDetectVel & 0xFFFF);
                result[26] = (ushort)((HomeSLITDetect >> 16) & 0xFFFF);
                result[27] = (ushort)(HomeSLITDetect & 0xFFFF);
                result[28] = (ushort)((HomeZSGDetect >> 16) & 0xFFFF);
                result[29] = (ushort)(HomeZSGDetect & 0xFFFF);
                result[30] = (ushort)((HomeOffset >> 16) & 0xFFFF);
                result[31] = (ushort)(HomeOffset & 0xFFFF);

                return result;
            }

            public int[] ToArray()
            {
                return new int[]
                {
                    JogDistance,
                    JogSpeed,
                    JogAccDec,
                    JogStartVel,
                    JogHighSpeed,
                    JogHomeOpCommandSTimeConst,
                    JogHomeOpTorqueLmt,
                    HomeType,
                    HomeDir,
                    HomeAccDec,
                    HomeStartVel,
                    HomeSpeed,
                    HomeDetectVel,
                    HomeSLITDetect,
                    HomeZSGDetect,
                    HomeOffset
                };
            }

            public void FromArray(int[] values)
            {
                if (values == null || values.Length != 16)
                    throw new ArgumentException("values 必須為 16 個 int 元素的陣列");
                JogDistance = values[0];
                JogSpeed = values[1];
                JogAccDec = values[2];
                JogStartVel = values[3];
                JogHighSpeed = values[4];
                JogHomeOpCommandSTimeConst = values[5];
                JogHomeOpTorqueLmt = values[6];
                HomeType = values[7];
                HomeDir = values[8];
                HomeAccDec = values[9];
                HomeStartVel = values[10];
                HomeSpeed = values[11];
                HomeDetectVel = values[12];
                HomeSLITDetect = values[13];
                HomeZSGDetect = values[14];
                HomeOffset = values[15];
            }

        }

        #endregion

        #region Motor Data Info

        public struct MotorOpDataDto
        {
            public int OpType { get; set; } = 1;
            public int Position { get; set; } = 0; // unit: step
            public int Velocity { get; set; } = 0; // unit: r/min

            public MotorOpDataDto() { }

            // 工具方法：合併高低 ushort 為 int，支援負數
            private static int CombineUShortToInt(ushort high, ushort low)
            {
                uint combined = ((uint)high << 16) | (uint)low;
                return unchecked((int)combined);
            }

            /// <summary>
            /// 從 ushort 陣列設定屬性值，陣列長度必須為 6，依序為 OpType 高低位、Position 高低位、Velocity 高低位
            /// </summary>
            /// <param name="setValue"></param>
            /// <exception cref="ArgumentException"></exception>
            public void FromUShortArray(ushort[] setValue)
            {
                if (setValue == null || setValue.Length != 6)
                    throw new ArgumentException("setValue 必須為 6 個 ushort 元素的陣列");
                // 合併高低位元，正確處理負數
                OpType = CombineUShortToInt(setValue[0], setValue[1]);
                Position = CombineUShortToInt(setValue[2], setValue[3]);
                Velocity = CombineUShortToInt(setValue[4], setValue[5]);
            }

            /// <summary>
            /// 從 int 陣列設定屬性值，陣列長度必須為 3，依序為 OpType、Position、Velocity
            /// </summary>
            /// <param name="values"></param>
            /// <exception cref="ArgumentException"></exception>
            public void FromIntArray(int[] values)
            {
                if (values == null || values.Length != 3)
                    throw new ArgumentException("values 必須為 3 個 int 元素的陣列");
                OpType = values[0];
                Position = values[1];
                Velocity = values[2];
            }

            /// <summary>
            /// 將屬性值轉換為 ushort 陣列，陣列長度為 6，依序為 OpType 高低位、Position 高低位、Velocity 高低位
            /// </summary>
            /// <returns></returns>
            public ushort[] ToUShortArray()
            {
                ushort[] result = new ushort[6];
                // 依序拆解每個 int 屬性為高低兩個 ushort
                result[0] = (ushort)((OpType >> 16) & 0xFFFF); // 高位
                result[1] = (ushort)(OpType & 0xFFFF);         // 低位
                result[2] = (ushort)((Position >> 16) & 0xFFFF);
                result[3] = (ushort)(Position & 0xFFFF);
                result[4] = (ushort)((Velocity >> 16) & 0xFFFF);
                result[5] = (ushort)(Velocity & 0xFFFF);
                return result;
            }

            /// <summary>
            /// 將 Position 屬性轉換為 ushort 陣列，陣列長度為 2，依序為 Position 高低位
            /// </summary>
            /// <returns></returns>
            public ushort[] ToPositionUShortArray()
            {
                ushort[] result = new ushort[2];
                // 依序拆解 Position 屬性為高低兩個 ushort
                result[0] = (ushort)((Position >> 16) & 0xFFFF);
                result[1] = (ushort)(Position & 0xFFFF);
                return result;
            }

            /// <summary>
            /// 將屬性值轉換為 int 陣列，陣列長度為 3，依序為 OpType、Position、Velocity
            /// </summary>
            /// <returns></returns>
            public int[] ToIntArray()
            {
                return new int[]
                {
                    OpType,
                    Position,
                    Velocity
                };
            }

        }

        #endregion


        #endregion

        /// <summary>
        /// 複製來源 MotorInfo 的基礎資訊到目前物件
        /// </summary>
        /// <param name="source"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CopyBaseInfo(MotorInfo source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Id = source.Id;
            IO_Input_High = source.IO_Input_High;
            IO_Input_Low = source.IO_Input_Low;
            IO_Output_High = source.IO_Output_High;
            IO_Output_Low = source.IO_Output_Low;
            Jog_Home_Setting = source.Jog_Home_Setting;
            ErrorCode = source.ErrorCode;
            Pos_Target = source.Pos_Target;
            Pos_Command = source.Pos_Command;
            Pos_Actual = source.Pos_Actual;
            Vel_Target = source.Vel_Target;
            Vel_Command = source.Vel_Command;
            Vel_Actual = source.Vel_Actual;
            ErrorComm = source.ErrorComm;
            OpData_IdSelect = source.OpData_IdSelect;
            OpData_IdOp = source.OpData_IdOp;
            OpData_Pos_Command = source.OpData_Pos_Command;
            OpData_VelR_Command = source.OpData_VelR_Command;
            OpData_Vel_Command = source.OpData_Vel_Command;
            OpData_Pos_Actual = source.OpData_Pos_Actual;
            OpData_VelR_Actual = source.OpData_VelR_Actual;
            OpData_Vel_Actual = source.OpData_Vel_Actual;
            OpData_Trq_Monitor = source.OpData_Trq_Monitor;
            OpData_Load_Monitor = source.OpData_Load_Monitor;
            CurrentDataNo = source.CurrentDataNo;

            JogMode = source.JogMode;
        }

        public void CopyOpDataArray(MotorOpDataDto[] sourceArray, bool isExArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));
            if (isExArray)
            {
                if (OpDataExArray == null || OpDataExArray.Length != sourceArray.Length)
                    OpDataExArray = new MotorOpDataDto[sourceArray.Length];
                Array.Copy(sourceArray, OpDataExArray, sourceArray.Length);
            }
            else
            {
                if (OpDataArray == null || OpDataArray.Length != sourceArray.Length)
                    OpDataArray = new MotorOpDataDto[sourceArray.Length];
                Array.Copy(sourceArray, OpDataArray, sourceArray.Length);
            }
        }
    }
}
