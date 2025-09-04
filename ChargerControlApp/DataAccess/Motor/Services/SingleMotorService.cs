using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Motor.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using System.Runtime.InteropServices;

namespace ChargerControlApp.DataAccess.Motor.Services
{
    public class SingleMotorService : IDisposable, ISingleMotorService
    {
        #region Read IO

        public Motor_IO_Input_High IO_Input_High;
        public Motor_IO_Input_Low IO_Input_Low;
        public Motor_IO_Output_High IO_Output_High;
        public Motor_IO_Output_Low IO_Output_Low;
        public int ErrorCode { get; internal set; } = 0;

        #endregion

        #region Read Pos and Vel

        public int Pos_Target { get; internal set; } = 0; // unit: step
        public int Pos_Command { get; internal set; } = 0; // unit: step
        public int Pos_Actual { get; internal set; } = 0; // unit: step
        public int Vel_Target { get; internal set; } = 0; // unit: r/min    
        public int Vel_Command { get; internal set; } = 0; // unit: r/min
        public int Vel_Actual { get; internal set; } = 0; // unit: r/min
        public int ErrorComm { get; internal set; } = 0;

        #endregion

        #region Read Operation Data

        public int OpData_IdSelect { get; internal set; } = 0;
        public int OpData_IdOp { get; internal set; } = 0;
        public int OpData_Pos_Command { get; internal set; } = 0; // unit: step
        public int OpData_VelR_Command { get; internal set; } = 0;  // unit: r/min
        public int OpData_Vel_Command { get; internal set; } = 0;  // unit: step/sec
        public int OpData_Pos_Actual { get; internal set; } = 0;  // unit: step
        public int OpData_VelR_Actual { get; internal set; } = 0;  // unit: r/min
        public int OpData_Vel_Actual { get; internal set; } = 0;  // unit: step/sec

        public int CurrentDataNo { get; set; } = 0;

        #endregion

        #region Read Jog Setting

        public int JogMode { get; internal set; } = 2;  // 0: Low Ppeed; 1; High Speed; 2: Pitch

        #endregion

        public byte SlaveAddress { get; set; } = 1;


        private IModbusRTUService _modbusRTUService;

        #region enum

        public enum ReadCommand
        { 
            ReadIO = 0, 
            WriteIO = 1,
        }

        #endregion

        #region constructor 

        private SingleMotorService() { }

        public SingleMotorService(IModbusRTUService modbusRTUService, byte slaveAddress)
        {
            _modbusRTUService = modbusRTUService;
            SlaveAddress = slaveAddress;

        }

        #endregion

        #region Route Process

        private int _routeIndex = 0;

        private MotorFrame[] _routeProcess = new MotorFrame[]
            {
                new MotorFrame()
                {
                    Id = 0, Name = "Read IO",
                    DataFrame = new ModbusRTUFrame()
                    {
                        FunctionCode = 0x03,
                        StartAddress = 124,
                        DataNumber = 6
                    }
                },
                new MotorFrame()
                {
                    Id = 0, Name = "Read Pos and Vel",
                    DataFrame = new ModbusRTUFrame()
                    {
                        FunctionCode = 0x03,
                        StartAddress = 150,
                        DataNumber = 14
                    }
                },
                new MotorFrame()
                {
                    Id = 0, Name = "Read Operation Data",
                    DataFrame = new ModbusRTUFrame()
                    {
                        FunctionCode = 0x03,
                        StartAddress = 194,
                        DataNumber = 16
                    }
                },
                new MotorFrame()
                {
                    Id = 0, Name = "Read Jog Setting",
                    DataFrame = new ModbusRTUFrame()
                    {
                        FunctionCode = 0x03,
                        StartAddress = 34848,
                        DataNumber = 4
                    }
                }
            };

        public async Task<bool> ExecuteRouteProcessOnce()
        {
            bool result = false;

            try
            {

                if (_modbusRTUService.IsRunning)
                {
                    _routeProcess[_routeIndex].DataFrame.SlaveAddress = SlaveAddress;
                    var data = _modbusRTUService.Act(_routeProcess[_routeIndex].DataFrame);
                    if (data.Result != null)
                    {
                        if (data.Result.Data.Length >= _routeProcess[_routeIndex].DataFrame.DataNumber)
                        {
                            if (_routeIndex == 0)
                            {
                                IO_Input_High.Data = data.Result.Data[0];
                                IO_Input_Low.Data = data.Result.Data[1];
                                IO_Output_High.Data = data.Result.Data[2];
                                IO_Output_Low.Data = data.Result.Data[3];

                                ErrorCode = (data.Result.Data[4] << 16) | data.Result.Data[5];
                            }
                            else if (_routeIndex == 1)
                            {
                                Pos_Target = (data.Result.Data[0] << 16) | data.Result.Data[1];
                                Pos_Command = (data.Result.Data[2] << 16) | data.Result.Data[3];
                                Pos_Actual = (data.Result.Data[4] << 16) | data.Result.Data[5];
                                Vel_Target = (data.Result.Data[6] << 16) | data.Result.Data[7];
                                Vel_Command = (data.Result.Data[8] << 16) | data.Result.Data[9];
                                Vel_Actual = (data.Result.Data[10] << 16) | data.Result.Data[11];
                                ErrorComm = (data.Result.Data[12] << 16) | data.Result.Data[13];

                            }
                            else if (_routeIndex == 2)
                            {
                                OpData_IdSelect = (data.Result.Data[0] << 16) | data.Result.Data[1];
                                OpData_IdOp = (data.Result.Data[2] << 16) | data.Result.Data[3];
                                OpData_Pos_Command = (data.Result.Data[4] << 16) | data.Result.Data[5];
                                OpData_VelR_Command = (data.Result.Data[6] << 16) | data.Result.Data[7];
                                OpData_Vel_Command = (data.Result.Data[8] << 16) | data.Result.Data[9];
                                OpData_Pos_Actual = (data.Result.Data[10] << 16) | data.Result.Data[11];
                                OpData_VelR_Actual = (data.Result.Data[12] << 16) | data.Result.Data[13];
                                OpData_Vel_Actual = (data.Result.Data[14] << 16) | data.Result.Data[15];
                            }
                            else if (_routeIndex == 3)
                            { 
                                int _mode_ori = (data.Result.Data[0] << 16) | data.Result.Data[1];

                                int _mode = 2;

                                if (_mode_ori == 48) _mode = 0;
                                else if (_mode_ori == 50) _mode = 1;

                                    JogMode = _mode;
                            }
                        }
                    }
                }

                if (++_routeIndex >= _routeProcess.Length)
                {
                    _routeIndex = 0;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                _routeIndex = 0;
            }

            return result;
        }

        public async Task<bool> WriteFrame(MotorFrame frame)
        {
            bool result = false;
            if (_modbusRTUService.IsRunning)
            {
                var data = _modbusRTUService.Act(frame.DataFrame);
                if (data.Result != null)
                {
                    if (data.Result.HasResponse == true)
                    {
                        result = true;
                    }
                }
            }
            return result;
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
        /// bit 10: R-OUT26 [CONST-OFF]
        /// bit 11: R-OUT27 [CONST-OFF]
        /// bit 12: R-OUT28 [CONST-OFF]
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
                public bool CONST_OFF => (Value & (1 << 10)) != 0;
                public bool CONST_OFF2 => (Value & (1 << 11)) != 0;
                public bool CONST_OFF3 => (Value & (1 << 12)) != 0;
                public bool CONST_OFF4 => (Value & (1 << 13)) != 0;
                public bool USR_OUT0 => (Value & (1 << 14)) != 0;
                public bool USR_OUT1 => (Value & (1 << 15)) != 0;
            }
        }/// <summary> 
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
