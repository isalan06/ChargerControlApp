using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.Hardware;

namespace ChargerControlApp.Controllers
{
    public class ChargerController : Controller
    {
        private readonly ILogger<ChargerController> _logger;
        private readonly HardwareManager _harewareManager;


        public ChargerController(ILogger<ChargerController> logger, HardwareManager hardwareManager)
        {
            _logger = logger;
            _harewareManager = hardwareManager;
        }
        public IActionResult Index()
        {
            ViewBag.NPB450ControllerInstnaceNumber = HardwareManager.NPB450ControllerInstnaceNumber;
            ViewBag.Chargers = _harewareManager.Charger; // hardwareManager �� HardwareManager ���
            return View();
        }

        [HttpPost]
        public IActionResult StartCharging([FromBody] ChargerIdDto dto)
        {
            var charger = _harewareManager.Charger.FirstOrDefault(c => c.deviceID == dto.id);
            charger?.StartCharging();
            return Ok();
        }

        [HttpPost]
        public IActionResult StopCharging([FromBody] ChargerIdDto dto)
        {
            var charger = _harewareManager.Charger.FirstOrDefault(c => c.deviceID == dto.id);
            charger?.StopCharging();
            return Ok();
        }

        [HttpGet]
        public JsonResult GetChargerStatus(int id)
        {
            var charger = _harewareManager.Charger.FirstOrDefault(c => c.deviceID == id);
            if (charger == null)
            {
                return Json(new
                {
                    voltage = "-",
                    current = "-",
                    fullm = false,
                    ccm = false,
                    cvm = false,
                    fvm = false,
                    wakeup_stop = false,
                    ntcer = false,
                    btnc = false,
                    cctof = false,
                    cvtof = false,
                    fvtof = false,
                    otp = false,
                    ovp = false,
                    olp = false,
                    short_ = false,
                    ac_fail = false,
                    op_off = false,
                    hi_temp = false
                });
            }

            return Json(new
            {
                voltage = charger.GetCachedVoltage(),
                current = charger.GetCachedCurrent(),
                fullm = charger.CHG_STATUS.Bits.FULLM,
                ccm = charger.CHG_STATUS.Bits.CCM,
                cvm = charger.CHG_STATUS.Bits.CVM,
                fvm = charger.CHG_STATUS.Bits.FVM,
                wakeup_stop = charger.CHG_STATUS.Bits.WAKEUP_STOP,
                ntcer = charger.CHG_STATUS.Bits.NTCER,
                btnc = charger.CHG_STATUS.Bits.BTNC,
                cctof = charger.CHG_STATUS.Bits.CCTOF,
                cvtof = charger.CHG_STATUS.Bits.CVTOF,
                fvtof = charger.CHG_STATUS.Bits.FVTOF,
                otp = charger.FAULT_STATUS.Bits.OTP,
                ovp = charger.FAULT_STATUS.Bits.OVP,
                olp = charger.FAULT_STATUS.Bits.OLP,
                short_ = charger.FAULT_STATUS.Bits.SHORT,
                ac_fail = charger.FAULT_STATUS.Bits.AC_FAIL,
                op_off = charger.FAULT_STATUS.Bits.OP_OFF,
                hi_temp = charger.FAULT_STATUS.Bits.HI_TEMP
            });
        }
    }

    public class ChargerIdDto
    {
        public int id { get; set; }
    }
}