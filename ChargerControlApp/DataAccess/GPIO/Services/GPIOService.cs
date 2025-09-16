using ChargerControlApp.DataAccess.GPIO.Models;
using System.Device.Gpio;

namespace ChargerControlApp.DataAccess.GPIO.Services
{
    public class GPIOService
    {
        // 靜態數值，模擬兩個 GPIO Input 的腳位狀態
        public static bool Pin1Value { get; set; } = false;
        public static bool Pin2Value { get; set; } = false;

        // 兩個 GPIO Input 的 GPIOInfo 實例
        public static GPIOInfo Pin1 { get; } = new GPIOInfo
        {
            PhysicalPinNumber = 11, // 例如 Pin 11 (GPIO17)
            BCMNumber = 17,
            Name = "GPIO17",
            IsInput = true,
            Value = Pin1Value
        };

        public static GPIOInfo Pin2 { get; } = new GPIOInfo
        {
            PhysicalPinNumber = 13, // 例如 Pin 13 (GPIO27)
            BCMNumber = 27,
            Name = "GPIO27",
            IsInput = true,
            Value = Pin2Value
        };

        // 內部 GpioController 實例
        private static GpioController? _controller;
        public static bool IsGpioAvailable => _controller != null;

        // 初始化 GPIO 控制器與腳位
        static GPIOService()
        {
            try
            {
                _controller = new GpioController();
                _controller.OpenPin(Pin1.BCMNumber, PinMode.Input);
                _controller.OpenPin(Pin2.BCMNumber, PinMode.Input);
            }
            catch
            {
                // 若在非樹莓派環境可忽略例外
                _controller = null;
            }
        }

        /// <summary>
        /// 從硬體讀取兩個 GPIO Input 的狀態並同步到靜態屬性與 GPIOInfo
        /// </summary>
        public static void ReadInputsFromHardware()
        {
            if (_controller != null)
            {
                Pin1Value = _controller.Read(Pin1.BCMNumber) == PinValue.High;
                Pin2Value = _controller.Read(Pin2.BCMNumber) == PinValue.High;
            }
            // 同步到 GPIOInfo
            Pin1.Value = Pin1Value;
            Pin2.Value = Pin2Value;
        }
    }
}
