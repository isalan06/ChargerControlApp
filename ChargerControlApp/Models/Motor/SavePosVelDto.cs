namespace ChargerControlApp.Models.Motor
{
    public class SavePosVelDto
    {
        public int MotorId { get; set; }
        public List<OpDataInput> OpDataList { get; set; }
        public class OpDataInput
        {
            public int Index { get; set; }
            public string Position { get; set; }
            public string Velocity { get; set; }
        }
    }
}
