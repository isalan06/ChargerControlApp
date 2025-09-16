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
                ClassName = this.ClassName
            };
        }
    }
}
