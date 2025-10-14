namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class PosFrame : ProcedureFrame
    {
        /// <summary>
        /// 軸編號 (0~2)
        /// </summary>
        public int AxisId { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 位置資料編號 (0~255)
        /// </summary>
        public int PosDataNo { get; set; }
        
        /// <summary>
        /// Gets or sets the current position within the sequence.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the delay time in milliseconds.
        /// When the robot reaches this position, it will wait for the specified time before proceeding to the next action.
        /// </summary>
        public double DelayTime_ms { get; set; } = 50.0;

        /// <summary>
        /// Gets or sets the timeout value in milliseconds.
        /// </summary>
        public double Timeout_ms {get;set;} = 120000.0;

        /// <summary>
        /// 建立此物件的淺層複製
        /// </summary>
        public override ProcedureFrame Clone()
        {
            return new PosFrame
            {
                AxisId = this.AxisId,
                Name = this.Name,
                Description = this.Description,
                PosDataNo = this.PosDataNo,
                ClassName = this.ClassName,
                Position = this.Position,
                DelayTime_ms = this.DelayTime_ms,
                Timeout_ms = this.Timeout_ms
            };
        }
    }
}
