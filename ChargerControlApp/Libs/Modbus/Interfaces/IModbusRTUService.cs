using ChargerControlApp.Libs.Modbus.Models;

namespace ChargerControlApp.Libs.Modbus.Interfaces
{
    public interface IModbusRTUService
    {
        bool Open();
        void Close();

       Task<ModbusRTUFrame> Act(ModbusRTUFrame coammand);
    }
}
