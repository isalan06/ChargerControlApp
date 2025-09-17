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
            },
            new MotorFrame()
            {
                Name = "ReadOpData",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "ReadOpData_No_0", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1800, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_1", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1840, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_2", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1880, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_3", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x18C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_4", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1900, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_5", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1940, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_6", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1980, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_7", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x19C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_8", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1A00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_9", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1A40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_10", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1A80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_11", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1AC0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_12", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1B00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_13", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1B40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_14", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1B80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_15", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1BC0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_16", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1C00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_17", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1C40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_18", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1C80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_19", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1CC0, DataNumber = 6 } }
                }
            },
            new MotorFrame()
            {
                Name = "WriteOpData",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "WriteOpData_No_0", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1800, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_1", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1840, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_2", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1880, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_3", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x18C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_4", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1900, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_5", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1940, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_6", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1980, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_7", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x19C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_8", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_9", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_10", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_11", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1AC0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_12", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_13", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_14", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_15", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1BC0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_16", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_17", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_18", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_19", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1CC0, DataNumber = 6 } }
                }
            }, 
            new MotorFrame()
            {
                Name = "WriteOpData_Position",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "WriteOpData_Position_No_0", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1802, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_1", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1842, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_2", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1882, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_3", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x18C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_4", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1902, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_5", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1942, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_6", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1982, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_7", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x19C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_8", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_9", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_10", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1A82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_11", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1AC2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_12", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_13", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_14", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1B82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_15", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1BC2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_16", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_17", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_18", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1C82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_19", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1CC2, DataNumber = 2 } }
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
            { "ReadOpData", Commands.First(c => c.Name == "ReadOpData") },
            { "WriteOpData", Commands.First(c => c.Name == "WriteOpData") },
            { "WriteOpData_Position", Commands.First(c => c.Name == "WriteOpData_Position") }
        };
    }
}
