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
            },
            new MotorFrame()
            { 
                Name = "WriteOpData_DefaultVelocityForJog",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "WriteOpData_DefaultVelocityForJog_No_0", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x4A04, DataNumber = 2, Data = new ushort[] { 0, 10 } } },
                    new MotorFrame { Name = "WriteOpData_DefaultVelocityForJog_No_1", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x4A44, DataNumber = 2, Data = new ushort[] { 0, 25 } } },
                    new MotorFrame { Name = "WriteOpData_DefaultVelocityForJog_No_2", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x4A84, DataNumber = 2, Data = new ushort[] { 0, 50 } } },
                    new MotorFrame { Name = "WriteOpData_DefaultVelocityForJog_No_3", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x4AC4, DataNumber = 2, Data = new ushort[] { 0, 100 } } },
                    new MotorFrame { Name = "WriteOpData_DefaultVelocityForJog_No_4", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x4B04, DataNumber = 2, Data = new ushort[] { 0, 300 } } }
                }
            },
            new MotorFrame()
            {
                Name = "WriteROutFunction_26to29",
                DataFrame = new ModbusRTUFrame()
                {
                    FunctionCode = 0x10,
                    StartAddress = 0x8874,
                    DataNumber = 8,
                    Data = new ushort[] { 0, 96, 0, 97, 0, 176, 0, 128 } // ROut26 = 96(R0_R), ROut27 = 97(R1_R), ROut28 = 176(HOME-END), ROut29 = 128(CONST-OFF)
                }
            },
            new MotorFrame()
            {
                Name = "ReadOpExData",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "ReadOpData_No_20", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1D00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_21", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1D40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_22", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1D80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_23", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1DC0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_24", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1E00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_25", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1E40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_26", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1E80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_27", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1EC0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_28", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1F00, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_29", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1F40, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_30", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1F80, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_31", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x1FC0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_32", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2000, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_33", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2040, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_34", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2080, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_35", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x20C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_36", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2100, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_37", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2140, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_38", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2180, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_39", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x21C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_40", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2200, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_41", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2240, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_42", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2280, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_43", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x22C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_44", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2300, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_45", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2340, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_46", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2380, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_47", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x23C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_48", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2400, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_49", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2440, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_50", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2480, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_51", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x24C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_52", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2500, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_53", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2540, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_54", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2580, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_55", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x25C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_56", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2600, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_57", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2640, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_58", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2680, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_59", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x26C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_60", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2700, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_61", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2740, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_62", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2780, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_63", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x27C0, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_64", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2800, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_65", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2840, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_66", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x2880, DataNumber = 6 } },
                    new MotorFrame { Name = "ReadOpData_No_67", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x03, StartAddress = 0x28C0, DataNumber = 6 } }
                }
            },
            new MotorFrame()
            {
                Name = "WriteOpExData",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "WriteOpData_No_20", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_21", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_22", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_23", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1DC0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_24", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_25", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_26", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_27", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1EC0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_28", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F00, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_29", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F40, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_30", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F80, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_31", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1FC0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_32", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2000, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_33", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2040, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_34", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2080, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_35", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x20C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_36", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2100, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_37", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2140, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_38", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2180, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_39", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x21C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_40", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2200, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_41", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2240, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_42", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2280, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_43", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x22C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_44", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2300, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_45", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2340, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_46", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2380, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_47", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x23C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_48", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2400, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_49", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2440, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_50", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2480, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_51", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x24C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_52", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2500, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_53", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2540, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_54", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2580, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_55", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x25C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_56", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2600, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_57", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2640, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_58", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2680, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_59", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x26C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_60", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2700, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_61", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2740, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_62", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2780, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_63", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x27C0, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_64", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2800, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_65", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2840, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_66", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2880, DataNumber = 6 } },
                    new MotorFrame { Name = "WriteOpData_No_67", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x28C0, DataNumber = 6 } }
                }
            },
            new MotorFrame()
            {
                Name = "WriteOpExData_Position",
                SubFrames = new List<MotorFrame>()
                {
                    new MotorFrame { Name = "WriteOpData_Position_No_20", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_21", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_22", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1D82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_23", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1DC2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_24", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_25", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_26", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1E82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_27", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1EC2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_28", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F02, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_29", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F42, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_30", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1F82, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_31", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x1FC2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_32", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2002, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_33", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2042, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_34", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2082, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_35", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x20C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_36", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2102, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_37", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2142, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_38", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2182, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_39", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x21C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_40", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2202, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_41", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2242, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_42", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2282, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_43", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x22C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_44", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2302, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_45", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2342, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_46", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2382, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_47", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x23C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_48", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2402, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_49", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2442, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_50", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2482, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_51", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x24C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_52", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2502, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_53", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2542, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_54", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2582, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_55", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x25C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_56", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2602, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_57", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2642, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_58", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2682, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_59", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x26C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_60", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2702, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_61", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2742, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_62", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2782, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_63", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x27C2, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_64", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2802, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_65", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2842, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_66", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x2882, DataNumber = 2 } },
                    new MotorFrame { Name = "WriteOpData_Position_No_67", DataFrame = new ModbusRTUFrame{ FunctionCode = 0x10, StartAddress = 0x28C2, DataNumber = 2 } }
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
            { "WriteOpData_Position", Commands.First(c => c.Name == "WriteOpData_Position") },
            { "WriteOpData_DefaultVelocityForJog", Commands.First(c => c.Name == "WriteOpData_DefaultVelocityForJog") },
            { "WriteROutFunction_26to29", Commands.First(c => c.Name == "WriteROutFunction_26to29") },
            { "ReadOpExData", Commands.First(c=>c.Name == "ReadOpExData") },
            { "WriteOpExData", Commands.First(c => c.Name == "WriteOpExData") },
            { "WriteOpExData_Position", Commands.First(c=>c.Name == "WriteOpExData_Position") }
        };
    }
}
