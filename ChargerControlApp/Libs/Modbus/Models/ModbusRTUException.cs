namespace ChargerControlApp.Libs.Modbus.Models
{
    public class ModbusRTUException: Exception
    {
        public byte SlaveAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte ExceptionCode { get; set; }
        public ModbusRTUException(string message, byte slaveAddress, byte functionCode, byte exceptionCode) : base(message)
        {
            SlaveAddress = slaveAddress;
            FunctionCode = functionCode;
            ExceptionCode = exceptionCode;
        }

        public override string ToString()
        {
            return $"ModbusRTUException: SlaveAddress={SlaveAddress}, FunctionCode={FunctionCode}, ExceptionCode={ExceptionCode}, Message={Message}";
        }
    }
}
