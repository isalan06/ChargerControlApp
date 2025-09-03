using ChargerControlApp.DataAccess.Modbus.Models;
using SocketCANSharp.Network.Netlink;

namespace ChargerControlApp.DataAccess.Motor.Models
{
    public static class MotorCommandList
    {

        public static List<MotorFrame> Commands = new List<MotorFrame>()
        {
            new MotorFrame()
            {
                Name = "WriteInputHigh",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x6,
                    StartAddress = 124,
                    DataNumber = 1
                }
            },
            new MotorFrame()
            {
                Name = "WriteInputLow",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x6,
                    StartAddress = 125,
                    DataNumber = 1
                }
            }

        }; 

        public static Dictionary<string, MotorFrame> CommandMap = new Dictionary<string, MotorFrame>()
        {
            { "WriteInputHigh", Commands.First(c => c.Name == "WriteInputHigh") },
            { "WriteInputLow", Commands.First(c => c.Name == "WriteInputLow") },

        };
    }
}
