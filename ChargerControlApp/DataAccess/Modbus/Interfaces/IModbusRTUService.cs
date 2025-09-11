using ChargerControlApp.DataAccess.Modbus.Models;
//using System.IO.Ports;
using RJCP.IO.Ports;

namespace ChargerControlApp.DataAccess.Modbus.Interfaces
{
    public interface IModbusRTUService
    {
        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }

        bool IsRunning { get; }

        int Timeout { get; set; }

        bool Open();
        void Close();

        Task<ModbusRTUFrame> Act(ModbusRTUFrame coammand);
    }
}
