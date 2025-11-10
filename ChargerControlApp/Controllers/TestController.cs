using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("Test")]
public class TestController : Controller
{
    private readonly RobotService _robotService;
    private readonly ChargingStationStateMachine _stateMachine;

    public TestController(RobotService robotService, ChargingStationStateMachine stateMachine)
    {
        _robotService = robotService;
        _stateMachine = stateMachine;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("StartTest1")]
    public IActionResult StartTest1([FromForm] int testExecuteIndex, [FromForm] int testCycleNumber)
    {
        // testProcedureIndex 固定為 1
        _robotService.StartTestAutoProcedure(1, testExecuteIndex, testCycleNumber);
        return Json(new { success = true, message = "Test1流程已啟動" });
    }

    [HttpPost("StopTest1")]
    public IActionResult StopTest1()
    {
        _robotService.StopAutoProcedure();
        return Json(new { success = true, message = "Test1流程已停止" });
    }

    [HttpGet("GetTestStatus")]
    public IActionResult GetTestStatus()
    {
        return new JsonResult(new
        {
            mainProcedureCase = _robotService.MainProcedureCase,
            errorMessage = _robotService.LastError?.ErrorMessage ?? "-",
            state = _stateMachine.GetCurrentStateName(),
            isTestMode = _robotService.IsTestMode,
            testCycleCount = _robotService.TestCycleCount,
        });
    }
}