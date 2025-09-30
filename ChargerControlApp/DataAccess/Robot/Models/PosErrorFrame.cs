namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class PosErrorFrame
    {
        public int AxisId { get; set; } = 0;

        public int ErrorCode { get; set; } = 0; 
        public string ErrorMessage { get; set; } = string.Empty;

        public PosErrorFrame Clone()
        {
            return new PosErrorFrame
            {
                AxisId = this.AxisId,
                ErrorCode = this.ErrorCode,
                ErrorMessage = this.ErrorMessage
            };
        }

        public void Clear()
        {
            AxisId = 0;
            ErrorCode = 0;
            ErrorMessage = string.Empty;
        }

        public override string ToString()
        {
            return $"AxisId: {AxisId}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}";
        }
    }
}
