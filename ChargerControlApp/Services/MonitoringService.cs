using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ChargerControlApp.Services
{
    public class MonitoringService : BackgroundService
    {
        private Timer _timer;
        private readonly HardwareManager _hardwareManager;
        private readonly ChargingStationStateMachine _stateMachine;
        private readonly RobotService _robotService;
        private readonly SlotServices _slotServices;

        public MonitoringService(HardwareManager hardwareManager, ChargingStationStateMachine stateMachine, RobotService robotService, SlotServices slotServices)
        {
            _hardwareManager = hardwareManager;
            _stateMachine = stateMachine;
            _robotService = robotService;
            _slotServices = slotServices;
        }

        private void CheckSystemStatus()
        {
            // 根據需要，可以加入更多的監控項目
            if (_stateMachine._currentState != null)
            {  
                if (_stateMachine._currentState.CurrentState == ChargingState.Swapping)
                {
                    if (_robotService.IsCriticalAlarm)
                    {
                        Console.WriteLine("❌ 換電程序中偵測到緊急警報，停止換電程序並轉為 Error 狀態");
                        _robotService.StopAutoProcedure();
                        _stateMachine.TransitionTo<ErrorState>();
                    }
                }

            }
        }

        public bool StartAutoProcedure()
        {
            if ((_stateMachine._currentState.CurrentState == ChargingState.Idle) && !_robotService.IsCriticalAlarm)// && _robotService.IsHomeFinished)
            {
                Console.WriteLine("✅ 開始自動換電程序");
                _robotService.StartAutoProcedure();
                _stateMachine.TransitionTo<SwappingState>();
                return true;
            }
            else
            {
                Console.WriteLine("❌ 無法開始自動換電程序，請確認目前狀態是否為 Idle 及 完成原點復歸 且無警報");
                return false;
            }
        }

        public bool StopAutoProcedure()
        {
            if (_stateMachine._currentState.CurrentState == ChargingState.Swapping)
            {
                Console.WriteLine("✅ 停止自動換電程序");
                _robotService.StopAutoProcedure();
                _stateMachine.TransitionTo<IdleState>();
                return true;
            }
            else
            {
                Console.WriteLine("❌ 無法停止自動換電程序，請確認目前狀態是否為 Swapping");
                return false;
            }
        }

        public void ResetAlarm()
        { 
            _robotService.ResetAlarm();

            if(_stateMachine._currentState.CurrentState == ChargingState.Error)
            {
                Console.WriteLine("✅ 警報已重置，轉為 Idle 狀態");
                _stateMachine.TransitionTo<IdleState>();
            }

            _slotServices.ResetAllAlarm();
        }

        public void Reset()
        {
            _robotService.ResetStatus();

            if(_stateMachine._currentState.CurrentState != ChargingState.Unspecified)
            {
                Console.WriteLine("✅ 系統已重置，轉為 Idle 狀態");
                _stateMachine.TransitionTo<IdleState>();
            }

            _slotServices.ResetAllSlotStatus();
        }

        public bool StartHomeProcedure()
        {
            if (!_robotService.IsCriticalAlarm && _robotService.CanHome && (_stateMachine._currentState.CurrentState == ChargingState.Idle))
            {
                Console.WriteLine("✅ 開始原點復歸程序");
                _robotService.StartHomeProcedure();
                return true;
            }
            else
            {
                Console.WriteLine("❌ 無法開始原點復歸程序，請確認目前狀態是否為 Idle 及 可以原點復歸 且無警報");
                return false;
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //_stateMachine.HandleTransition(ChargingState.Initial);

            while (!stoppingToken.IsCancellationRequested)
            {
                CheckSystemStatus();

                // 你的監控邏輯
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
