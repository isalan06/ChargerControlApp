using ChargerControlApp.DataAccess.Modbus.Models;

namespace ChargerControlApp.DataAccess.Modbus.Interfaces
{
    public interface IModbusRTUService
    {
        bool Open();
        void Close();

        Task<ModbusRTUFrame> Act(ModbusRTUFrame coammand);
    }
}
