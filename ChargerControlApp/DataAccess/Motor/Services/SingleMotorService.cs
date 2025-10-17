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

        public byte SlaveAddress { get; set; } = 1;


        private IModbusRTUService _modbusRTUService;

        public bool IsHomeFinished { get; set; } = false;


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
                    var data = await _modbusRTUService.Act(_routeProcess[_routeIndex].DataFrame);
                    if (data != null)
                    {
                        if (data.Data.Length >= _routeProcess[_routeIndex].DataFrame.DataNumber)
                        {
                            if (_routeIndex == 0)
                            {
                                MotorInfo.IO_Input_High.Data = data.Data[0];
                                MotorInfo.IO_Input_Low.Data = data.Data[1];
                                MotorInfo.IO_Output_High.Data = data.Data[2];
                                MotorInfo.IO_Output_Low.Data = data.Data[3];

                                MotorInfo.ErrorCode = (data.Data[4] << 16) | data.Data[5];
                            }
                            else if (_routeIndex == 1)
                            {
                                MotorInfo.Pos_Target = CombineInt32(data.Data[0], data.Data[1]);
                                MotorInfo.Pos_Command = CombineInt32(data.Data[2], data.Data[3]);
                                MotorInfo.Pos_Actual = CombineInt32(data.Data[4], data.Data[5]);
                                MotorInfo.Vel_Target = CombineInt32(data.Data[6], data.Data[7]);
                                MotorInfo.Vel_Command = CombineInt32(data.Data[8], data.Data[9]);
                                MotorInfo.Vel_Actual = CombineInt32(data.Data[10], data.Data[11]);
                                MotorInfo.ErrorComm = (data.Data[12] << 16) | data.Data[13];

                            }
                            else if (_routeIndex == 2)
                            {
                                MotorInfo.OpData_IdSelect = (data.Data[0] << 16) | data.Data[1];
                                MotorInfo.OpData_IdOp = (data.Data[2] << 16) | data.Data[3];
                                MotorInfo.OpData_Pos_Command = CombineInt32(data.Data[4], data.Data[5]);
                                MotorInfo.OpData_VelR_Command = CombineInt32(data.Data[6], data.Data[7]);
                                MotorInfo.OpData_Vel_Command = CombineInt32(data.Data[8], data.Data[9]);
                                MotorInfo.OpData_Pos_Actual = CombineInt32(data.Data[10], data.Data[11]);
                                MotorInfo.OpData_VelR_Actual = CombineInt32(data.Data[12], data.Data[13]);
                                MotorInfo.OpData_Vel_Actual = CombineInt32(data.Data[14], data.Data[15]);
                            }
                            else if (_routeIndex == 3)
                            { 
                                int _mode_ori = (data.Data[0] << 16) | data.Data[1];

                                int _mode = 2;

                                if (_mode_ori == 48) _mode = 0;
                                else if (_mode_ori == 50) _mode = 1;

                                MotorInfo.JogMode = _mode;
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
