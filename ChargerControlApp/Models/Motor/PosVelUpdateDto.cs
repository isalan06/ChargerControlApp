namespace ChargerControlApp.Models.Motor
{
    public class PosVelUpdateDto
    {
        public int MotorId { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
