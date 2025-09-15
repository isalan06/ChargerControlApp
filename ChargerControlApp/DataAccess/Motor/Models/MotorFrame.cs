using ChargerControlApp.DataAccess.Modbus.Models;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorFrame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ModbusRTUFrame DataFrame { get; set; } = new ModbusRTUFrame();

        public MotorFrame Clone()
        {
            return new MotorFrame
            {
                Id = this.Id,
                Name = this.Name,
                DataFrame = new ModbusRTUFrame
                {
                    SlaveAddress = this.DataFrame.SlaveAddress,
                    FunctionCode = this.DataFrame.FunctionCode,
                    StartAddress = this.DataFrame.StartAddress,
                    DataNumber = this.DataFrame.DataNumber,
                    Data = (ushort[])this.DataFrame.Data?.Clone()
                }
            };
        }
    }
}
