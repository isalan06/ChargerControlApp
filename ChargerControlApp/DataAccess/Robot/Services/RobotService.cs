using ChargerControlApp.Hardware;

namespace ChargerControlApp.DataAccess.Robot.Services
{
    public class RobotService : IDisposable
    {
        private readonly HardwareManager hardwareManager;


        #region constructor

        public RobotService(IServiceProvider serviceProvider)
        {
            // 取得 HardwareManager 實例
            hardwareManager = serviceProvider.GetRequiredService<HardwareManager>();
        }

        #endregion

        #region IDisposable & Destructor

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
        ~RobotService()
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
    }
}
