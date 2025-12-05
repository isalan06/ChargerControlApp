using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Motor.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using System.Runtime.InteropServices;

namespace ChargerControlApp.DataAccess.Motor.Services
{
    public class SingleMotorService : IDisposable, ISingleMotorService
    {
        public MotorInfo MotorInfo { get; set; } = new MotorInfo();
        private MotorInfo _motorInfoBuffer { get; set; } = new MotorInfo();

        public byte SlaveAddress { get; set; } = 1;

        private IModbusRTUService _modbusRTUService;

        public bool IsHomeFinished { get; set; } = false;

        private SingleMotorPersistence? _persistence = null;


        #region enum

        public enum ReadCommand
        { 
            ReadIO = 0, 
            WriteIO = 1,
        }

        #endregion

        #region constructor 

        private SingleMotorService() { }

        public SingleMotorService(IModbusRTUService modbusRTUService, byte slaveAddress, int id)
        {
            _modbusRTUService = modbusRTUService;
            SlaveAddress = slaveAddress;
            MotorInfo.Id = id;
            _persistence = new SingleMotorPersistence(id);
        }

        #endregion

        #region Route Process

        private int _routeIndex = 0;

        /// <summary>
        /// 路由處理程序
        /// </summary>
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
                        DataNumber = 24,
                        FinalCommand = true
                    }
                }
                //new MotorFrame() // 暫時不讀
                //{
                //    Id = 0, Name = "Read Jog Setting",
                //    DataFrame = new ModbusRTUFrame()
                //    {
                //        FunctionCode = 0x03,
                //        StartAddress = 34848,
                //        DataNumber = 4
                //    }
                //}
            };

        public async Task<bool> ExecuteRouteProcessOnce()
        {
            bool result = false;

            try
            {
                if (_modbusRTUService.IsRunning)
                {
                    _routeProcess[_routeIndex].DataFrame.SlaveAddress = SlaveAddress;
                    var data = await _modbusRTUService.Act(_routeProcess[_routeIndex].DataFrame);
                    if (data != null)
                    {
                        if (data.Data.Length >= _routeProcess[_routeIndex].DataFrame.DataNumber)
                        {
                            if (_routeIndex == 0)
                            {
                                _motorInfoBuffer.IO_Input_High.Data = data.Data[0];
                                _motorInfoBuffer.IO_Input_Low.Data = data.Data[1];
                                _motorInfoBuffer.IO_Output_High.Data = data.Data[2];
                                _motorInfoBuffer.IO_Output_Low.Data = data.Data[3];

                                _motorInfoBuffer.ErrorCode = (data.Data[4] << 16) | data.Data[5];
                            }
                            else if (_routeIndex == 1)
                            {
                                _motorInfoBuffer.Pos_Target = CombineInt32(data.Data[0], data.Data[1]);
                                _motorInfoBuffer.Pos_Command = CombineInt32(data.Data[2], data.Data[3]);
                                _motorInfoBuffer.Pos_Actual = CombineInt32(data.Data[4], data.Data[5]);
                                _motorInfoBuffer.Vel_Target = CombineInt32(data.Data[6], data.Data[7]);
                                _motorInfoBuffer.Vel_Command = CombineInt32(data.Data[8], data.Data[9]);
                                _motorInfoBuffer.Vel_Actual = CombineInt32(data.Data[10], data.Data[11]);
                                _motorInfoBuffer.ErrorComm = (data.Data[12] << 16) | data.Data[13];

                            }
                            else if (_routeIndex == 2)
                            {
                                _motorInfoBuffer.OpData_IdSelect = (data.Data[0] << 16) | data.Data[1];
                                _motorInfoBuffer.OpData_IdOp = (data.Data[2] << 16) | data.Data[3];
                                _motorInfoBuffer.OpData_Pos_Command = CombineInt32(data.Data[4], data.Data[5]);
                                _motorInfoBuffer.OpData_VelR_Command = CombineInt32(data.Data[6], data.Data[7]);
                                _motorInfoBuffer.OpData_Vel_Command = CombineInt32(data.Data[8], data.Data[9]);
                                _motorInfoBuffer.OpData_Pos_Actual = CombineInt32(data.Data[10], data.Data[11]);
                                _motorInfoBuffer.OpData_VelR_Actual = CombineInt32(data.Data[12], data.Data[13]);
                                _motorInfoBuffer.OpData_Vel_Actual = CombineInt32(data.Data[14], data.Data[15]);
                                _motorInfoBuffer.OpData_Trq_Monitor = Convert.ToDouble(CombineInt32(data.Data[20], data.Data[21])) / 10.0;
                                _motorInfoBuffer.OpData_Load_Monitor = Convert.ToDouble(CombineInt32(data.Data[22], data.Data[23])) / 10.0;
                            }
                            else if (_routeIndex == 3)
                            {
                                // 原本讀取JOG模式，但目前不使用此功能，先註解掉
                                //int _mode_ori = (data.Data[0] << 16) | data.Data[1];

                                //int _mode = 2;

                                //if (_mode_ori == 48) _mode = 0;
                                //else if (_mode_ori == 50) _mode = 1;

                                //_motorInfoBuffer.JogMode = _mode;
                            }

                            // 如果是最後一筆資料讀取，則進行資料轉移
                            if (data.FinalCommand)
                            {
                                MotorInfo.CopyBaseInfo(_motorInfoBuffer);
                                
                                Console.WriteLine($"\r\nMotor-{MotorInfo.Id} Info retrieved from BLDC Driver:\r\n");
                                Console.WriteLine($"Set Pos No: {MotorInfo.CurrentDataNo}; Real Pos No: {MotorInfo.CurrentPosNo}; Real Pos Value: {MotorInfo.Pos_Actual}; Start Trigger: {MotorInfo.IO_Input_High.Bits.START};\r\n");
                                Console.WriteLine($"RDY_SD_OPE: {MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE}; MOVE: {MotorInfo.IO_Output_Low.Bits.MOVE}; IN_POS: {MotorInfo.IO_Output_Low.Bits.IN_POS};\r\n");

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
                var data = await _modbusRTUService.Act(frame.DataFrame);
                if (data!= null)
                {
                    if (data.HasResponse == true)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public async Task<ushort[]> ReadFrame(MotorFrame frame)
        {
            ushort[] result = Array.Empty<ushort>();
            if (_modbusRTUService.IsRunning)
            {
                var data = await _modbusRTUService.Act(frame.DataFrame);
                if (data != null)
                {
                    if (data.HasResponse == true)
                    {
                        result = data.Data;
                    }
                }
            }
            return result;
        }

        #endregion

        private int CombineInt32(ushort high, ushort low)
        {
            uint value = ((uint)high << 16) | (uint)low;
            // 判斷最高位元是否為 1（負數）
            if ((value & 0x80000000) != 0)
                return (int)(value - 0x100000000);
            else
                return (int)value;
        }

        public bool LoadPersistence()
        {
            bool result = false;
            if (_persistence != null)
            {
                if(_persistence.IsFileExist)
                {
                    var opData = _persistence.Load();
                    if (opData.Length > 0)
                    {
                        Array.Copy(opData, MotorInfo.OpDataArray, opData.Length);
                        result = true;
                    }
                }
            }
            return result;
        }
        public bool LoadExPersistence()
        {
            bool result = false;
            if (_persistence != null)
            {
                if (_persistence.IsFileExExist)
                {
                    var opDataEx = _persistence.LoadEx();
                    if (opDataEx.Length > 0)
                    {
                        Array.Copy(opDataEx, MotorInfo.OpDataExArray, opDataEx.Length);
                        result = true;
                    }
                }
            }
            return result;
        }

        public void SavePersistence()
        {
            if (_persistence != null)
            {
                _persistence.Save(MotorInfo.OpDataArray);
            }
        }

        public void SaveExPersistence()
        {
            if (_persistence != null)
            {
                _persistence.SaveEx(MotorInfo.OpDataExArray);
            }
        }

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
