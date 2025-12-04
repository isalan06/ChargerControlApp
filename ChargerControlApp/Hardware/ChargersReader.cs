using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using System.Runtime.InteropServices;
using static ChargerControlApp.Hardware.NPB450Controller;

namespace ChargerControlApp.Hardware
{
    public class ChargersReader : IDisposable
    {
        private readonly ICANBusService _canBusService;
        private readonly HardwareManager _hardwareManager;
        public bool IsRunning { get; internal set; } = false;

        public static byte[]? ReceivedCANBusMessage = null;
        public static int ChargerIndex { get; set; } = -1;
        public static int ChargerCommandData { get; set; } = -1;

        public static int ChargerResponseIndex { get; internal set; } = -1;
        public static int ChargerResponseData { get; internal set; } = -1;

        #region costructor

        private ChargersReader()
        { }

        public ChargersReader(ICANBusService canBusService, HardwareManager hardwareManager) : this()
        {
            _canBusService = canBusService;
            
            _hardwareManager = hardwareManager;

            Open();
        }

        #endregion

        #region Disposable Support and Destructor

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
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ChargersReader()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Task

        private CancellationTokenSource source = new CancellationTokenSource();

        private Task DoWork()
        {
            CancellationToken ct = source.Token;

            return Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        try
                        {
                            uint canid = 0x123; // Example CAN ID
                            ushort commandCode = 0x0; // Example command code
                            ReceivedCANBusMessage = _canBusService.ReceiveMessageWithID(ref canid, ref commandCode);
                           
                            if (ReceivedCANBusMessage != null)
                            {
                                int chargerIndex = (int)(canid& 0xFF); // Assuming charger index is in the lower byte of CAN ID
                                var cmd = (NPB450Controller.CanbusReadCommand)commandCode;
                                switch (cmd)
                                {
                                    case NPB450Controller.CanbusReadCommand.READ_VOUT:
                                        // 處理 READ_VOUT
                                        byte[]? VoltageBytes = ReceivedCANBusMessage; // 取得收到的資料
                                        if (VoltageBytes == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetVoltage_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].Voltage = (double)Voltage / 100;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetVoltage_Sync()-Voltage: {_hardwareManager.Charger[chargerIndex].Voltage} V");
                                        }
                                        break;

                                    case NPB450Controller.CanbusReadCommand.READ_IOUT:
                                        // 處理 READ_IOUT
                                        byte[]? CurrentBytes = ReceivedCANBusMessage; // 取得收到的資料
                                        if (CurrentBytes == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetCurrent_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].Current = (double)Current / 100;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetCurrent_Sync()-Current: {_hardwareManager.Charger[chargerIndex].Current} A");
                                        }
                                        break;
                                        

                                    case NPB450Controller.CanbusReadCommand.CHG_STATUS:
                                        // 處理 CHG_STATUS
                                        byte[]? CHG_STATUS_BYTES = ReceivedCANBusMessage; // 取得收到的資料
                                        if (CHG_STATUS_BYTES == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetCHG_STATUS_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            CHG_STATUS_Union CHG_STATUS = new CHG_STATUS_Union();
                                            CHG_STATUS.Data = BitConverter.ToUInt16(new byte[] { CHG_STATUS_BYTES[2], CHG_STATUS_BYTES[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].CHG_STATUS = CHG_STATUS;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetCHG_STATUS_Sync()-CHG_STATUS: 0x{_hardwareManager.Charger[chargerIndex].CHG_STATUS.Data:X}");
                                        }
                                        break;

                                    case NPB450Controller.CanbusReadCommand.FAULT_STATUS:
                                        // 處理 FAULT_STATUS
                                        byte[]? FAULT_STATUS_BYTES = ChargersReader.ReceivedCANBusMessage; // 取得收到的資料
                                        if (FAULT_STATUS_BYTES == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetFAULT_STATUS_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        FAULT_STATUS_Union FAULT_STATUS = new FAULT_STATUS_Union();
                                        FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);
                                        _hardwareManager.Charger[chargerIndex].FAULT_STATUS = FAULT_STATUS;
                                        _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                        Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetFAULT_STATUS_Sync()-FAULT_STATUS: 0x{_hardwareManager.Charger[chargerIndex].FAULT_STATUS.Data:X}");
                                        break;

                                    default:
                                        Console.WriteLine($"No Canbus Route Command = 0x{commandCode:X}");
                                        break;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error receiving CAN bus message: {ex.Message}");
                        }
                    }
                }
            }, ct);
        }

        #endregion

        #region Command

        public void Open()
        {
            IsRunning = true;
            DoWork();
        }

        public void Close()
        {
            IsRunning = false;
        }

        #endregion
    }
}
