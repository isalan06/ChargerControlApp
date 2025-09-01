using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Modbus.Models;
using System;
using System.IO.Ports;


namespace ChargerControlApp.DataAccess.Modbus.Services
{
    public class ModbusRTUService : IModbusRTUService, IDisposable
    {
        private SerialPort _serialPort = new SerialPort();
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.Even;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;

        public bool IsRunning { get; internal set; } = false;

        public int Timeout { get; set; } = 1000;

        private bool _readResult = false;
        private ushort[] _readData = null;
        private List<byte> _buffer = new List<byte>();



        #region Constructor

        public ModbusRTUService() { _serialPort.DataReceived += _serialPort_DataReceived; }



        public ModbusRTUService(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One) : this()
        {
            PortName = portName; BaudRate = baudRate; Parity = parity; DataBits = dataBits; StopBits = stopBits;
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
                    source.Cancel();
                    IsRunning = false;
                    _serialPort.Dispose();
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ModbusRTUService()
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

        #region Events

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int number = _serialPort.BytesToRead;

            if (number > 0)
            {
                byte[] _data = new byte[number];

                _serialPort.Read(_data, 0, _data.Length);

                _buffer.AddRange(_data);

            }
        }

        #endregion

        #region Task

        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken ct = source.Token;

        private Task DoWork()
        {
            return Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    //Console.WriteLine("ModbusRTUService is running...");
                    // Your periodic work here




                    Thread.Sleep(10); // Adjust the delay as needed
                }
            }, ct);
        }

        #endregion

        #region Functions

        public bool Open()
        {
            bool result = false;

            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.PortName = PortName;
                    _serialPort.BaudRate = BaudRate;
                    _serialPort.Parity = Parity;
                    _serialPort.DataBits = DataBits;
                    _serialPort.StopBits = StopBits;
                    _serialPort.ReadTimeout = 1000;
                    _serialPort.WriteTimeout = 1000;
                    _serialPort.Open();
                    result = true;
                    IsRunning = true;
                    DoWork();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }

            return result;
        }
        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    IsRunning = false;

                    _serialPort.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }

            #endregion
        }

        public async Task<ModbusRTUFrame> Act(ModbusRTUFrame coammand)
        {
            ModbusRTUFrame _frame = null;

            if (_serialPort.IsOpen)
            {
                _frame = new ModbusRTUFrame(coammand);
                _frame.HasResponse = false;
                _frame.HasException = false;
                _readResult = false;
                _buffer.Clear();

                var _command = _frame.CreateCommand();
                _serialPort.Write(_command, 0, _command.Length);

                bool _timeout = false;

                DateTime dt = DateTime.Now;

                while (!_timeout)
                {
                    TimeSpan ts = DateTime.Now.Subtract(dt);

                    if (ts.TotalMilliseconds >= Timeout) break;

                    try
                    {
                        if (_frame.AnalizeResponse(_buffer.ToArray()))
                        {
                            _frame.HasResponse = true;
                            break;
                        }
                    }
                    catch (ModbusRTUException ex)
                    {
                        _frame.HasException = true;
                        Console.WriteLine(ex.Message);
                        break;
                    }

                    Thread.Sleep(10);
                }

            }

            return _frame;
        }
    }
}
