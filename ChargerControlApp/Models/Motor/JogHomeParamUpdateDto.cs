namespace ChargerControlApp.Models.Motor
{
    // DTO for 單一參數更新
    public class JogHomeParamUpdateDto
    {
        public int MotorId { get; set; }
        public int Index { get; set; }
        public string Value { get; set; }
    }
}
