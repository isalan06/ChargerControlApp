using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using System.Runtime.InteropServices;

namespace ChargerControlApp.Hardware
{
    public class ChargersReader_old : IDisposable
    {
        private readonly ICANBusService _canBusService;
        public bool IsRunning { get; internal set; } = false;

        public static byte[]? ReceivedCANBusMessage = null;
        public static int ChargerIndex { get; set; } = -1;
        public static int ChargerCommandData { get; set; } = -1;

        public static int ChargerResponseIndex { get; internal set; } = -1;
        public static int ChargerResponseData { get; internal set; } = -1;

        #region costructor

        private ChargersReader_old()
        { }

        public ChargersReader_old(ICANBusService canBusService) : this()
        {
            _canBusService = canBusService;
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
        ~ChargersReader_old()
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
                bool ExecutedOnce = false; // Flag to ensure initialization runs only once

                while (!ct.IsCancellationRequested && IsRunning)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        //if ((ChargerCommandData != ChargerResponseData) || (ChargerIndex != ChargerResponseIndex))
                        //{
                        try
                        {
                            ReceivedCANBusMessage = _canBusService.ReceiveMessage();
                            Console.WriteLine($"Received CAN bus message for Charger Index: {ChargerIndex}, Command Data: {ChargerCommandData}");
                            if (ReceivedCANBusMessage != null)
                            {
                                ChargerResponseIndex = ChargerIndex;
                                ChargerResponseData = ChargerCommandData;
                                ChargerIndex = -1;
                                ChargerCommandData = -1;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error receiving CAN bus message: {ex.Message}");
                        }
                        //}

                        //await Task.Delay(10); // Adjust the delay as needed
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
