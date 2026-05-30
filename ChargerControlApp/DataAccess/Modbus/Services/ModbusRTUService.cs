using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Modbus.Models;
using System;
using System.IO.Ports;
//using RJCP.IO.Ports;


namespace ChargerControlApp.DataAccess.Modbus.Services
{
    public class ModbusRTUService : IModbusRTUService, IDisposable
    {
        //private SerialPortStream _serialPort = new SerialPortStream();
        private SerialPort? _serialPort = new SerialPort();
        private readonly object _bufferLock = new object();
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } =230400;
        public Parity Parity { get; set; } = Parity.Even;
        public int DataBits { get; set; } =8;
        public StopBits StopBits { get; set; } = StopBits.One;

        public bool IsRunning { get; internal set; } = false;

        public int Timeout { get; set; } =500;

        private bool _readResult = false;
        private ushort[] _readData = new ushort[0];
        private List<byte> _buffer = new List<byte>();

        public bool IsConnected { get { return _serialPort != null ? _serialPort.IsOpen : false; } }

        private CancellationTokenSource source = new CancellationTokenSource();

        public static double InterFrameActMilliseconds =0.0; // Modbus RTU inter-frame Act in milliseconds
        public static double FrameReadMilliseconds =0.0; // Modbus RTU frame read in milliseconds

        private DateTime dt_next = DateTime.Now;

        private bool _openedOnce = false; // 是否曾經成功開啟過一次
        private bool _disconnectedShowOnce = false; // 是否已經顯示過一次斷線訊息

        #region Constructor

        public ModbusRTUService()
        {
            _serialPort.DataReceived += _serialPort_DataReceived;
        }



        public ModbusRTUService(string portName, int baudRate =230400, Parity parity = Parity.Even, int dataBits =8, StopBits stopBits = StopBits.One) : this()
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
                    _serialPort?.Dispose();
                }

                // TODO:釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)'具有會釋出非受控資源的程式碼時，才覆寫完成項
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
            int number =0;
            try
            {
                if (_serialPort != null)
                    number = _serialPort.BytesToRead;
            }
            catch (Exception)
            {
                // Port may have been closed/disconnected; ignore
                return;
            }

            if (number >0)
            {
                byte[] _data = new byte[number];

                try
                {
                    _serialPort?.Read(_data,0, _data.Length);
                }
                catch (Exception)
                {
                    // Read failed due to port closure or IO error
                    return;
                }

                lock (_bufferLock)
                {
                    _buffer.AddRange(_data);
                }

                //Console.WriteLine($"Received: {BitConverter.ToString(_data)}");

            }
        }

        #endregion

        #region Task

        private Task DoWork()
        {

            CancellationToken ct = source.Token;
            return Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    //Console.WriteLine("ModbusRTUService is running...");
                    // Your periodic work here
                    try
                    {

                        bool connectedStatus = _serialPort != null ? _serialPort.IsOpen : false;


                        if (_openedOnce && !connectedStatus)
                        {
                            if (!_disconnectedShowOnce)
                            {
                                Console.WriteLine($"ModbusRTUService: COM port {PortName} disconnected.");
                                _disconnectedShowOnce = true;
                            }
                            //IsRunning = false; // 停止服務

                            if (SerialPort.GetPortNames().Contains(PortName))
                            {
                                if (Reopen())
                                {
                                    _disconnectedShowOnce = false;
                                    Console.WriteLine($"ModbusRTUService: COM port {PortName} is available again. Attempting to reconnect...");
                                }
                            }

                            //break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ModbusRTUService DoWork Exception: {ex.ToString()}");
                    }


                    Thread.Sleep(10); // Adjust the delay as needed
                }

                Console.WriteLine($"Modbus RTU Server DoWork Finished: {ct.IsCancellationRequested}");
            }, ct);
        }

        #endregion

        #region Functions

        public bool Open()
        {
            bool result = false;

            if (_serialPort != null && !_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.PortName = PortName;
                    _serialPort.BaudRate = BaudRate;
                    _serialPort.Parity = Parity;
                    _serialPort.DataBits = DataBits;
                    _serialPort.StopBits = StopBits;
                    _serialPort.ReadTimeout =1000;
                    _serialPort.WriteTimeout =1000;
                    _serialPort.Open();
                    result = true;
                    IsRunning = true;
                    DoWork();
                    _openedOnce = true;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"COM Port List: {string.Join(", ", System.IO.Ports.SerialPort.GetPortNames())}");
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }

            return result;
        }
        public bool Reopen()
        {
            bool result = false;

            if (_serialPort == null || !_serialPort.IsOpen)
            {
                try
                {
                    if (_serialPort == null) _serialPort = new SerialPort();
                    _serialPort.PortName = PortName;
                    _serialPort.BaudRate = BaudRate;
                    _serialPort.Parity = Parity;
                    _serialPort.DataBits = DataBits;
                    _serialPort.StopBits = StopBits;
                    _serialPort.ReadTimeout = 1000;
                    _serialPort.WriteTimeout = 1000;
                    _serialPort.DataReceived += _serialPort_DataReceived;
                    _serialPort.Open();
                    result = true;
                    IsRunning = true;
                    _openedOnce = true;
                    Console.WriteLine($"Serial Port {PortName} Reopen....");

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Reopen Exception: {ex.ToString()} ;COM Port List: {string.Join(", ", System.IO.Ports.SerialPort.GetPortNames())}");
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }

            return result;
        }
        public void Close()
        {
            if (_serialPort != null && _serialPort.IsOpen)
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
        }
        public void CloseForException()
        {
            try
            {
                _serialPort?.Dispose();
                _serialPort = null;
                Console.WriteLine($"Serial Port {PortName} Close for Exception.....");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close For Exception: {ex.ToString()}");
            }
        }

        #endregion

        public async Task<ModbusRTUFrame> Act(ModbusRTUFrame command)
        {
            ModbusRTUFrame _frame = null;

            try
            {
                if (command.EmptyCommand)
                {
                    _frame = new ModbusRTUFrame(command);
                    _frame.DataNumber =0;
                    _frame.HasResponse = true;
                    return _frame;
                }

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    // 清空所有狀態
                    try
                    {
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                    }
                    catch (Exception) { /* ignore if port was closed */ }

                    lock (_bufferLock)
                    {
                        _buffer.Clear();
                    }

                    _readResult = false;
                    _readData = null;

                    _frame = new ModbusRTUFrame(command);
                    _frame.HasResponse = false;
                    _frame.HasException = false;


                    var _command = _frame.CreateCommand();

                    DateTime dt = DateTime.Now;

                    try
                    {
                        // Perform write and catch common failure modes explicitly
                        _serialPort.Write(_command,0, _command.Length);
                    }
                    catch (TimeoutException tex)
                    {
                        Console.WriteLine($"Act Write TimeoutException: {tex.Message}");
                        _frame.HasException = true;
                        try { CloseForException(); } catch { }
                        return _frame;
                    }
                    catch (OperationCanceledException oce)
                    {
                        Console.WriteLine($"Act Write OperationCanceledException: {oce.Message}");
                        _frame.HasException = true;
                        try { CloseForException(); } catch { }
                        return _frame;
                    }
                    catch (IOException ioe)
                    {
                        Console.WriteLine($"Act Write IOException: {ioe.Message}");
                        _frame.HasException = true;
                        try { CloseForException(); } catch { }
                        return _frame;
                    }
                    catch (InvalidOperationException ioe2)
                    {
                        Console.WriteLine($"Act Write InvalidOperationException: {ioe2.Message}");
                        _frame.HasException = true;
                        try { CloseForException(); } catch { }
                        return _frame;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Act Write Exception: {ex.Message}");
                        _frame.HasException = true;
                        try { CloseForException(); } catch { }
                        return _frame;
                    }

                    bool _timeout = false;


                    while (!_timeout)
                    {
                        TimeSpan ts = DateTime.Now.Subtract(dt);

                        if (ts.TotalMilliseconds >= Timeout)
                        {
                            _timeout = true;
                            _frame.HasException = true;
                            byte[] snapshot;
                            lock (_bufferLock)
                            {
                                snapshot = _buffer.ToArray();
                            }
                            Console.WriteLine($"Act Timeout: {ts.TotalMilliseconds} ms; Data: {BitConverter.ToString(snapshot)}");
                            break;
                        }

                        try
                        {
                            byte[] snapshot;
                            lock (_bufferLock)
                            {
                                snapshot = _buffer.ToArray();
                            }

                            if (_frame.AnalizeResponse(snapshot))
                            {
                                _frame.HasResponse = true;
                                break;
                            }
                        }
                        catch (ModbusRTUException ex)
                        {
                            _frame.HasException = true;
                            Console.WriteLine($"Act ModbusRTUException: {ex.Message}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _frame.HasException = true;
                            Console.WriteLine($"Act Exception: {ex.Message}");
                            break;
                        }

                        //Thread.Sleep(10);
                        await Task.Delay(10);
                    }

                    FrameReadMilliseconds = DateTime.Now.Subtract(dt).TotalMilliseconds; // 記錄讀取時間
                    InterFrameActMilliseconds = DateTime.Now.Subtract(dt_next).TotalMilliseconds; // 記錄與上次Act的間隔時間
                    dt_next = DateTime.Now; // 更新下一次的時間點

                }
            }
            catch (Exception ex)
            {
                try { CloseForException(); } catch { }
                Console.WriteLine($"Act Global Exception: {ex.ToString()}");
                var token = source.Token;
                bool aaa = token.IsCancellationRequested;
            }

            return _frame;
        }
    }
}
