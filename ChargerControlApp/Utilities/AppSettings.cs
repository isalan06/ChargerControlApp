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
        public string DeviceId { get; set; }
        public string IPPort { get; set; }
        public string DeviceModel { get; set; }
        public List<HardwareVersion> HwVersions { get; set; }
        public List<SoftwareVersion> SwVersions { get; set; }
        public string DeviceInfo { get; set; }
        public string TagName { get; set; }
        public string DeviceName { get; set; }
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

    public class HardwareVersion
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string SerialNumber { get; set; }
    }

    public class SoftwareVersion
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
