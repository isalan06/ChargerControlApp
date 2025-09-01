using System.IO.Ports;
using Smart.Modbus;

namespace ChargerControlApp.Test.Modbus
{
    public interface IMyModbusTesting
    {
    
    }

    public class MyModbusTesting : IMyModbusTesting, IDisposable
    {

        #region Serial Information
        private SerialPort _serialPort = new SerialPort();
        private string _portName = "/dev/ttySC0";


        
        public int BaudRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.Even;

        private bool _result = false;
        public ushort[] ReceivedData { get; private set; } = null;


        #endregion


        #region constructor

        public MyModbusTesting()
        {
#if DEBUG
            _portName = "COM1";
#endif

            _serialPort.DataReceived += _serialPort_DataReceived;
        }

        

        #endregion

        #region destuctor

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    Close();
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~MyModbusTesting()
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

        private List<byte> _receiveBuffer = new List<byte>();

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            if (_serialPort.BytesToRead > 0)
            {
                byte[] buffer = new byte[_serialPort.BytesToRead];
                _serialPort.Read(buffer, 0, buffer.Length);
                _receiveBuffer.AddRange(buffer);

                var a = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, 0x01);
                var result = a.ParseRegistersResponse(_receiveBuffer.ToArray());
                

                if (result != null)
                {
                    if(result.Length > 0)
                    {
                        Console.WriteLine("Data Received");
                        ReceivedData = new ushort[result.Length];
                        Array.Copy(result, ReceivedData, result.Length);
                        _result = true;
                    }
                }
            }

            //var a = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, 0x01);
            //a.ParseRegistersResponse(new byte[] { 0x01, 0x03, 0x02, 0x00, 0x0A, 0xC4, 0x0B });
        }

        #endregion

        #region Functions

        public void Open()
        { 
            _serialPort.PortName = _portName;
            _serialPort.BaudRate = BaudRate;
            _serialPort.Parity = Parity;


            if(_serialPort.IsOpen == false)
            {
                try
                {
                    _serialPort.Open();
                    Console.WriteLine($"Serial Port {_portName} Open Success!!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void Close()
        {
            Console.WriteLine($"Serial Port {_portName} Close!!");
            if (_serialPort.IsOpen)
            {
                Console.WriteLine("Closing Serial Port...");
                try
                {
                    _serialPort.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Console.WriteLine("Disposing Serial Port...");
                    _serialPort.Dispose();
                }
            }
        }

        public async Task<ushort[]> Read()
        { 
            
            _result = false;
            ReceivedData = null;
            //CancellationTokenSource s_cts = new CancellationTokenSource();

            var commandBuilder = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, 0x01);
            var command = commandBuilder.BuildReadHoldingRegistersRequest(0x7C, 4);
            

            if(_serialPort.IsOpen == true)
            {
                Console.WriteLine("Sending command...");
                _serialPort.Write(command, 0, command.Length);

                try
                {
                    //s_cts.CancelAfter(10000);
                    int count = 0;
                    Console.WriteLine("Waiting for data...");
                    while ((_result == false) && (count >= 20))
                    {
                        Console.WriteLine($"Waiting...{count}");
                        await Task.Delay(100);
                        count++;
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Read Timeout");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Console.WriteLine($"Result: {_result}");
                    //s_cts.Dispose();
                }

            }

            return ReceivedData;

        }

        public void Test()
        {

            

            var a = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, 0x01);
            //var result = a.ParseRegistersResponse(new byte[] { 0x01, 0x03, 0x02, 0x00, 0x0A, 0xC4, 0x0B });
            var result = a.ParseRegistersResponse(new byte[] { 0x01, 0x03, 0x02, 0x00});
            for (int i = 0; i < result.Length; i++)
            {
                Console.WriteLine($"Register {i}: {result[i]}");
            }
        }

        public void Test2()
        {
           


            var data1 = new byte[] { 0x01, 0x03, 0x02, 0x00, 0x0A, 0xC4, 0x0B };
            var data2 = new byte[] { 0x01, 0x03, 0x02, 0x00 };
            var data3 = new byte[] { 0x0A, 0xC4, 0x0B };

            var err_data1 = new byte[] { 0x05, 0x06, 0x07 };

            var data4 = new byte[] { 0x02, 0x03, 0x02, 0x00 };
            var data5 = new byte[] { 0x0A, 0xC4, 0x0B };

            var a = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, 0x01);


            var data_error = new byte[] { 0x01, 0x83, 0x02, 0xC0, 0xF1 };
            ushort[] data_error_data = a.ParseRegistersResponse(data_error);

            var data6 = new byte[] { 0x01, 0x03, 0x04, 0x12, 0x34, 0x56, 0x78, 0xB9, 0xF1 };

            byte[] list_data4 = a.FetchData(data6);

            var result3 = a.ValidateResponse(list_data4, FunctionCode.ReadHoldingRegisters);

            var result3_data = a.ParseRegistersResponse(list_data4);


            byte[] list_data = a.FetchData(data2);
            list_data = a.FetchData(data3);

            var result = a.ValidateResponse(list_data);

            var result_data = a.ParseRegistersResponse(list_data);


            byte[] list_data3 = a.FetchData(data4);
            list_data3 = a.FetchData(data5);

            var result2 = a.ValidateResponse(list_data3);
            var result2_data = a.ParseRegistersResponse(list_data3);

            byte[] list_data2 = a.FetchData(data4);
            list_data2 = a.FetchData(err_data1);
            list_data2 = a.FetchData(data5);

            
            


        }

        #endregion


    }
}
