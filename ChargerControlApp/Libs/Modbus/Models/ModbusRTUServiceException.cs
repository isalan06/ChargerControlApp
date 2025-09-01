namespace ChargerControlApp.Libs.Modbus.Models
{
    public class ModbusRTUServiceException: Exception
    {
        public string PortName { get; set; }

        public ModbusRTUServiceException(string message, string portName) : base(message)
        {
            PortName = portName;
        }

        public override string ToString()
        {
            return $"ModbusRTUServiceException: PortName={PortName}, Message={Message}";
        }
    }
}
