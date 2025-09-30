namespace ChargerControlApp.Models.Motor
{
    public class SetPositionRequest
    {
        public int MotorId { get; set; }
        public int PosIndex { get; set; }
        public int Position { get; set; }
    }
}
