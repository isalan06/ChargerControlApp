namespace ChargerControlApp.Models.Motor
{
    public class JogHomeParamBatchUpdateDto
    {
        public int MotorId { get; set; }
        public List<string> Values { get; set; }
    }
}
