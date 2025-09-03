using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

using Smart.Modbus;

namespace ChargerControlApp.DataAccess.Modbus.Models
{
    public class ModbusRTUFrame
    {
        public byte SlaveAddress { get; set; }
        public byte FunctionCode { get; set; }
        public ushort StartAddress { get; set; }
        public ushort DataNumber { get; set; }
        public ushort[] Data { get; set; } = Array.Empty<ushort>();

        public bool HasResponse { get; set; } = false;
        public bool HasException { get; set; } = false;
        public bool IsRead { get; internal set; } = false;
        public bool IsWrite { get; internal set; } = false;


        public ModbusRTUFrame()
        {
            SlaveAddress = 1;
            FunctionCode = 0x03; // Read Holding Registers
            StartAddress = 0;
            DataNumber = 1;
        }
        public ModbusRTUFrame(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[] data)
        {
            Set(slaveAddress, functionCode, startAddress, dataNumber, data);
        }

        public ModbusRTUFrame(ModbusRTUFrame frame)
        {
            this.Clone(frame);
        }

        public byte[] CreateCommand()
        {
            var modbusHandler = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, SlaveAddress);
            byte[] result = null;

            if (Data == null)
            {
                throw new ModbusRTUException("Data array is null", SlaveAddress, FunctionCode, 0xF0);
            }

            switch (FunctionCode)
            {
                case 0x03: // Read Holding Registers
                    result = modbusHandler.BuildReadHoldingRegistersRequest(StartAddress, DataNumber);
                    break;

                case 0x06: // Write Single Register
                    if (Data.Length >= 1)
                    {
                        result = modbusHandler.BuildWriteSingleRegisterRequest(StartAddress, Data[0]);
                    }
                    else
                    {
                        throw new ModbusRTUException("Data array must contain at least 1 elements for Write Single Register", SlaveAddress, FunctionCode, 0xF3);
                    }
                    break;
            }

            return result;

        }

        public bool AnalizeResponse(byte[] response)
        {
            var modbusHandler = Smart.Modbus.ModbusFactory.Create(ModbusProtocol.ModbusRTU, SlaveAddress);
            bool result = false;

            if (response != null)
            {
                if (response.Length >= 3)
                {
                    if (response[0] != SlaveAddress)
                        throw new ModbusRTUException($"Invalid Slave Address {response[0]} in response and the correct Slave Address {SlaveAddress}", response[0], response[1], 0xF1);

                    if ((response[1] & 0x80) != 0)
                    {
                        // Exception response
                        byte exceptionCode = response[2];
                        throw new ModbusRTUException($"Modbus Exception Code: {exceptionCode}", response[0], response[1], exceptionCode);
                    }
                    else if ((response[1] == 0x3) || ((response[1] == 0x4)))
                    {
                        ushort[] registers = modbusHandler.ParseRegistersResponse(response);
                        if (registers.Length == DataNumber)
                        {
                            Data = new ushort[DataNumber];
                            Array.Copy(registers, Data, DataNumber);
                            result = true;
                        }
                        else if (registers.Length > DataNumber)
                        {
                            throw new ModbusRTUException($"Invalid number of registers in response. Expected {DataNumber}, but got {registers.Length}", response[0], response[1], 0xF2);
                        }
                    }
                    else if ((response[1] == 0x6))
                    {
                        var valid = modbusHandler.ValidateResponse(response, Smart.Modbus.FunctionCode.WriteSingleRegister);
                        if (valid.Valid)
                        {
                            result = true;
                        }

                    }
                }
            }

            return result;
        }

        public void Set(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[] data, bool hasResponse=false, bool hasException=false)
        {
            SlaveAddress = slaveAddress;
            FunctionCode = functionCode;
            StartAddress = startAddress;
            DataNumber = dataNumber;
            HasResponse = hasResponse;
            HasException = hasException;
            if (data != null)
            {
                Data = new ushort[data.Length];
                Array.Copy(data, Data, data.Length);
            }

            bool _read = false;
            if ((functionCode == 0x1) || (functionCode == 0x2) || (functionCode == 0x3) || (functionCode == 0x4)) _read = true;

            IsRead = _read;
            IsWrite = !_read;

        }
        public void Set(ushort[] data)
        {
            if (data != null)
            {
                Data = new ushort[data.Length];
                Array.Copy(data, Data, data.Length);
            }
            else
                Data = null;
        }

        public void Clone(ModbusRTUFrame frame)
        {
            Set(frame.SlaveAddress, frame.FunctionCode, frame.StartAddress, frame.DataNumber, frame.Data, frame.HasResponse, frame.HasException);
        }
    }
}
