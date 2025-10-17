using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChargerControlApp.Utilities
{
    public class AppSettings
    {
        public string ServerIp { get; set; }
        public string ChargingStationName { get; set; }
        public int MaxChargingCurrent { get; set; }
        public string CanInterface { get; set; }
        public int CanBitrate { get; set; }
        public string PortName { get; set; }
        public string PortNameLinux { get; set; }

        public int PowerSupplyInstanceNumber { get; set; }

        public int PositionInPosOffset { get; set; }

        public bool SensorCheckPass { get; set; }

        public bool ServoOnAndHomeAfterStartup { get; set; }

        public bool ChargerUseAsync { get; set; }
    }
}
