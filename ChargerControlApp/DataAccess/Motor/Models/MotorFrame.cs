using ChargerControlApp.DataAccess.Modbus.Models;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorFrame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ModbusRTUFrame DataFrame { get; set; } = new ModbusRTUFrame();

        // 新增：可選的子命令清單
        public List<MotorFrame>? SubFrames { get; set; }

        public MotorFrame Clone()
        {
            return new MotorFrame
            {
                Id = this.Id,
                Name = this.Name,
                DataFrame = this.DataFrame != null ? new ModbusRTUFrame(this.DataFrame) : null,
                SubFrames = this.SubFrames?.Select(sf => sf.Clone()).ToList()
                /*DataFrame = new ModbusRTUFrame
                {
                    SlaveAddress = this.DataFrame.SlaveAddress,
                    FunctionCode = this.DataFrame.FunctionCode,
                    StartAddress = this.DataFrame.StartAddress,
                    DataNumber = this.DataFrame.DataNumber,
                    Data = (ushort[])this.DataFrame.Data?.Clone()
                }*/
            };
        }
    }
}
