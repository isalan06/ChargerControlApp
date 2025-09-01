using ChargerControlApp.DataAccess.Modbus.Models;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorFrame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ModbusRTUFrame DataFrame { get; set; } = new ModbusRTUFrame();
    }
}
