namespace ChargerControlApp.DataAccess.Robot.Models
{
    public class SensorFrame : ProcedureFrame
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
        /// 感測器名稱
        /// </summary>
        public string SensorName { get; set; } = string.Empty;

        /// <summary>
        /// 感測器的預期狀態
        /// </summary>
        public bool CheckStatus { get; set; } = false;


        /// <summary>
        ///  建立此物件的淺層複製
        /// </summary>
        /// <returns></returns>
        public override ProcedureFrame Clone()
        {
            return new SensorFrame
            {
                AxisId = this.AxisId,
                Name = this.Name,
                Description = this.Description,
                SensorName = this.SensorName,
                CheckStatus = this.CheckStatus,
                ClassName = this.ClassName
            };
        }
    }
}
