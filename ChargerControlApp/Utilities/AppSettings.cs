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

        public bool GRPCRegisterOnlyResponse { get; set; }

        public bool CheckBattaryExistByMemory { get; set; }

        public double CheckBatteryExistValue_Voltage_V { get; set; }

        public double CheckBatteryChargeValue_Voltage_V { get; set; }

        public double CheckBatteryFullChargeValue_A { get; set; }

        public long RechargeAfterFullDischarge_Minutes { get; set; }

        public long FullChargeCheckDelay_Seconds { get; set; }
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
