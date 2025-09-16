namespace ChargerControlApp.DataAccess.Robot.Models
{
    /// <summary>
    /// 基礎 Frame，供不同延伸類別共用，可放入同一個 List 中。
    /// </summary>
    public class ProcedureFrame
    {
        /// <summary>
        /// 實體的 Class 名稱
        /// </summary>
        public string ClassName { get; set; }

        public ProcedureFrame()
        {
            ClassName = GetType().Name;
        }

        /// <summary>
        /// 建立此物件的淺層複製
        /// </summary>
        public virtual ProcedureFrame Clone()
        {
            return new ProcedureFrame
            {
                ClassName = this.ClassName
            };
        }
    }
}
