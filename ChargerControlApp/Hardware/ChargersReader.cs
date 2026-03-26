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

        // 新增：儲存背景工作 Task，方便關閉時等待
        private Task? _backgroundTask;

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
                    // 取消背景工作並嘗試等待（短暫）
                    try
                    {
                        source.Cancel();
                        IsRunning = false;
                        if (_backgroundTask != null)
                        {
                            // 同步等待最多 1 秒，避免長時間 block
                            _backgroundTask.Wait(1000);
                        }
                    }
                    catch (AggregateException) { }
                    catch (Exception) { }
                }

                disposedValue = true;
            }
        }

        ~ChargersReader()
        {
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Task

        private CancellationTokenSource source = new CancellationTokenSource();

        private Task DoWork()
        {
            CancellationToken ct = source.Token;

            // 回傳 Task.Run 的 Task 物件給呼叫者以便等待
            return Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        try
                        {
                            uint canid = 0x123;
                            ushort commandCode = 0x0;
                            // 這裡仍呼叫同步介面 (ReceiveMessageWithID)，保留既有邏輯
                            ReceivedCANBusMessage = _canBusService.ReceiveMessageWithID(ref canid, ref commandCode);

                            if (ReceivedCANBusMessage != null)
                            {
                                int chargerIndex = (int)(canid & 0xFF);
                                var cmd = (NPB450Controller.CanbusReadCommand)commandCode;
                                switch (cmd)
                                {
                                    case NPB450Controller.CanbusReadCommand.READ_VOUT:
                                        byte[]? VoltageBytes = ReceivedCANBusMessage;
                                        if (VoltageBytes == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetVoltage_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].Voltage = (double)Voltage / 100;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                        }
                                        break;

                                    case NPB450Controller.CanbusReadCommand.READ_IOUT:
                                        byte[]? CurrentBytes = ReceivedCANBusMessage;
                                        if (CurrentBytes == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetCurrent_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].Current = (double)Current / 100;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                        }
                                        break;

                                    case NPB450Controller.CanbusReadCommand.CHG_STATUS:
                                        byte[]? CHG_STATUS_BYTES = ReceivedCANBusMessage;
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
                                        }
                                        break;

                                    case NPB450Controller.CanbusReadCommand.FAULT_STATUS:
                                        byte[]? FAULT_STATUS_BYTES = ReceivedCANBusMessage;
                                        if (FAULT_STATUS_BYTES == null)
                                        {
                                            Console.WriteLine($"NPB450Controller{chargerIndex}-[Linux]-GetFAULT_STATUS_Sync()-ReceivedCANBusMessage is null");
                                        }
                                        else
                                        {
                                            FAULT_STATUS_Union FAULT_STATUS = new FAULT_STATUS_Union();
                                            FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);
                                            _hardwareManager.Charger[chargerIndex].FAULT_STATUS = FAULT_STATUS;
                                            _hardwareManager.Charger[chargerIndex].RoutueCommandFrames.CaptureResponse(cmd);
                                        }
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

                    // 每輪小延遲，讓取消更敏感
                    try { await Task.Delay(5, ct); } catch (TaskCanceledException) { break; }
                }
            }, ct);
        }

        #endregion

        #region Command

        public void Open()
        {
            IsRunning = true;
            // 儲存背景 Task 供關閉時等待
            _backgroundTask = DoWork();
        }

        public void Close()
        {
            IsRunning = false;
            source.Cancel();
            // 不做長時間 blocking，Dispose 會嘗試等待短暫時間
        }

        #endregion
    }
}
