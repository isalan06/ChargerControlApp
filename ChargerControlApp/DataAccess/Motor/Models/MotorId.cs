namespace ChargerControlApp.DataAccess.Motor.Models
{
    public class MotorId
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte SlaveAddress { get; set; }
    }
}
