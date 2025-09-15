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
            },
            new MotorFrame()
            {
                Name = "WriteJogMode",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x10,
                    StartAddress = 34848,
                    DataNumber = 4
                }
            },
            new MotorFrame()
            { 
                Name = "ReadJogAndHomeSetting",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x03,
                    StartAddress = 672,
                    DataNumber = 32
                }
            },
            new MotorFrame()
            { 
                Name = "WriteJogAndHomeSetting",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x10,
                    StartAddress = 672,
                    DataNumber = 32
                }
            }

        }; 

        public static Dictionary<string, MotorFrame> CommandMap = new Dictionary<string, MotorFrame>()
        {
            { "WriteInputHigh", Commands.First(c => c.Name == "WriteInputHigh") },
            { "WriteInputLow", Commands.First(c => c.Name == "WriteInputLow") },
            { "WriteJogMode", Commands.First(c => c.Name == "WriteJogMode") },
            { "ReadJogAndHomeSetting", Commands.First(c => c.Name == "ReadJogAndHomeSetting") },
            { "WriteJogAndHomeSetting", Commands.First(c => c.Name == "WriteJogAndHomeSetting") },
        };
    }
}
