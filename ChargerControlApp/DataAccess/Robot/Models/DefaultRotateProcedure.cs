namespace ChargerControlApp.DataAccess.Robot.Models
{
    public static class DefaultRotateProcedure
    {
        private static int r_TargetPosDataNo = 0;
        public static int R_TargetPosDataNo // 旋轉軸目標位置編號
        {
            get
            {
                return r_TargetPosDataNo;
            }
            set
            {
                r_TargetPosDataNo = value;
                var posFrame = (PosFrame)ProcedureFrames[2];
                posFrame.PosDataNo = value;
            }

        }
        /// <summary>
        /// 預設旋轉動作流程
        /// </summary>
        public static List<ProcedureFrame> ProcedureFrames = new List<ProcedureFrame>
        {
            new PosFrame()
            {
                AxisId = 1,
                Name = "Y0",
                Description = "Y軸回到等待點",
                PosDataNo = 0,
            },
            new PosFrame()
            {
                AxisId = 2,
                Name = "Z0",
                Description = "Z軸回到等待點",
                PosDataNo = 0,
            },
            new PosFrame()
            {
                AxisId = 0,
                Name = "R-Target",
                Description = "旋轉軸旋轉到目標方向方向",
                PosDataNo = R_TargetPosDataNo,
            },
        };
    }
}
