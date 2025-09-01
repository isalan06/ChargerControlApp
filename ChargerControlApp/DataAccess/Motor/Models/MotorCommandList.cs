using ChargerControlApp.DataAccess.Modbus.Models;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public static class MotorCommandList
    {
        public static List<MotorFrame> Commands = new List<MotorFrame>()
        {
            new MotorFrame()
            {
                Id = 1,
                Name = "Read IO",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 3,
                    StartAddress = 124,
                    DataNumber = 4
                }
            }

        }; 
    }
}
