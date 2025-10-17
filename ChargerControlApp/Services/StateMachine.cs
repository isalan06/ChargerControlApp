using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Services;
using ChargerControlApp.Utilities;
using Grpc.Core;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TAC.Hardware;
using TacDynamics.Kernel.DeviceService.Protos;
using static ChargerControlApp.Services.InitialState;

namespace ChargerControlApp.Services
{
    public enum ChargingState
    {
        Unspecified,
        Initial,
        Idle,
        Swapping,
        Manual,
        Error
    }

    public abstract class State<T> where T : Enum
    {
        protected ChargingStationStateMachine _context;
        protected IServiceProvider _serviceProvider;
        protected ChargingState _stateEnum;
        protected T _currentState;
        public T CurrentState => _currentState;
        public void SetContext(ChargingStationStateMachine context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }
        public virtual void TransistTo(T newState)
        {
            Console.WriteLine($"狀態轉換: {_currentState} → {newState}");
            _currentState = newState;
        }
        public virtual void EnterState() { }
        public virtual void ExitState() { }
        public T GetCurrentStete() => _currentState;
        public ChargingState GetStateEnum() => _stateEnum;
        public bool IsCurrentState(T state) => _currentState.Equals(state);
        public abstract void HandleTransition(ChargingState nextState);

    }

    public class UnspecifiedState : State<ChargingState>
    {
        public UnspecifiedState()
        {
            _stateEnum = ChargingState.Unspecified;
            _currentState = ChargingState.Unspecified;
        }

        public override void EnterState()
        {
            Console.WriteLine("進入 Unspecified 狀態: 初始狀態");
            SlotServices.StationState = StationState.Unspecified;
        }
        public override void HandleTransition(ChargingState nextState)
        {
            if (nextState == ChargingState.Initial)
            {
                _context.TransitionTo<InitialState>();
            }
            else
            {
                Console.WriteLine("無效的狀態轉換");
            }
        }
    }

    public class InitialState : State<ChargingState>
    {
        int bypassCounter = 0;
        //private readonly GrpcClientService _grpcClientService;  // 已通過構造函數注入
        private readonly AppSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        //public InitializationState(GrpcClientService grpcClientService)
        //{
        //    _grpcClientService = grpcClientService;
        //    ConfigLoader.Load();
        //    _settings = ConfigLoader.GetSettings();
        //    _settings = _settings ?? new AppSettings();
        //}
        public InitialState(IServiceProvider serviceProvider)
        {
            _stateEnum = ChargingState.Initial;
            _currentState = ChargingState.Initial;
            _serviceProvider = serviceProvider;
        }

        public override async void EnterState()
        {
            Console.WriteLine("進入 Initialization 狀態: 檢查設定，FMS 註冊，原點復歸");
            SlotServices.StationState = StationState.Initial;
            // 將非同步操作移到外部方法中
            await InitializeAsync();
        }

        private async Task InitializeAsync()  // 改為返回 Task，方便處理異常
        {
            string responseDeviceName = "";
            await Task.Delay(5000); // 初步延遲
            bool result = true;

            var _hardwareManager = _serviceProvider.GetRequiredService<HardwareManager>();
            var _robotService = _serviceProvider.GetRequiredService<RobotService>();

            DevicePostRegistrationResponse devicePostRegistrationResponse;
            do
            {
                bypassCounter++;
                if (bypassCounter > 3)
                    break;
                try
                {
                    //devicePostRegistrationResponse = await _grpcClientService.RegisterDeviceAsync();
                    //Console.WriteLine($"收到註冊響應: {devicePostRegistrationResponse.DeviceName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"註冊過程中發生錯誤: {ex.Message}");
                    //return; // 錯誤時退出
                    break; // 先跳出迴圈，進入 Idle 狀態 => ToDo: 待上傳gRPC完成後再修改
                }

                await Task.Delay(5000); // 進行輪詢
            } while (false);//devicePostRegistrationResponse.DeviceName != _settings.ChargingStationName); // ToDo: 

            if (HardwareManager.ServoOnAndHomeAfterStartup) // 啟動後是否啟用伺服並回原點
            {
                try
                {
                    _hardwareManager.Robot.SetAllServo(true);
                    await Task.Delay(2000);
                    _robotService.StartHomeProcedure();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"機械手臂伺服啟動或回原點失敗: {ex.Message}");
                    result = false;
                }

                // 加入 timeout 機制
                var timeout = TimeSpan.FromSeconds(180); // 設定最大等待 180 秒
                var sw = Stopwatch.StartNew();
                while (!_robotService.IsHomeFinished)
                {
                    if (sw.Elapsed > timeout)
                    {
                        Console.WriteLine("原點復歸逾時，跳出等待迴圈");
                        result = false;
                        break;
                    }
                    await Task.Delay(500);
                }
            }



            // 原點復歸後，轉換到 Idle 狀態; 若失敗則轉換到 Error 狀態
            if (result)
                HandleTransition(ChargingState.Idle);// _context.TransitionTo<IdleState>();
            else
                HandleTransition(ChargingState.Error); // _context.TransitionTo<ErrorState>();
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Idle:
                    _context.TransitionTo<IdleState>();
                    break;
                case ChargingState.Error:
                    _context.TransitionTo<ErrorState>();
                    break;
                default:
                    Console.WriteLine("無效的狀態轉換");
                    break;
            }
        }

    }
    // Idle 狀態
    public class IdleState : State<ChargingState>
    {
        public IdleState()
        {
            _stateEnum = ChargingState.Idle;
            _currentState = ChargingState.Idle;
        }
        public override void EnterState()
        {
            Console.WriteLine("進入 Idle 狀態: 等待車輛進入");
            SlotServices.StationState = StationState.Idle;
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Swapping:
                    _context.TransitionTo<SwappingState>();
                    break;
                case ChargingState.Manual:
                    _context.TransitionTo<ManualState>();
                    break;
                case ChargingState.Error:
                    _context.TransitionTo<ErrorState>();
                    break;
                case ChargingState.Initial:
                    _context.TransitionTo<InitialState>();
                    break;
                default:
                    Console.WriteLine("無效的狀態轉換");
                    break;
            }
        }
    }

    // Swapping 狀態: 自動換電池流程
    public class SwappingState : State<ChargingState>
    {
        public SwappingState()
        {
            _stateEnum = ChargingState.Swapping;
            _currentState = ChargingState.Swapping;
        }

        public override void EnterState()
        {
            Console.WriteLine("進入 Swapping 狀態: 啟動自動流程");
            SlotServices.StationState = StationState.Swapping;

            // ToDo: 執行自動換電池流程

        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Idle:
                    _context.TransitionTo<IdleState>();
                    break;
                case ChargingState.Manual:
                    _context.TransitionTo<ManualState>();
                    break;
                case ChargingState.Error:
                    _context.TransitionTo<ErrorState>();
                    break;
                default:
                    Console.WriteLine("無效的狀態轉換");
                    break;
            }
        }
    }

    // Manual 狀態
    public class ManualState : State<ChargingState>
    {
        public ManualState()
        {
            _stateEnum = ChargingState.Manual;
            _currentState = ChargingState.Manual;
        }
        public override void EnterState()
        {
            Console.WriteLine("進入 Manual 狀態");
            SlotServices.StationState = StationState.Manual;
        }

        public override void HandleTransition(ChargingState nextState)
        {
            if (nextState == ChargingState.Idle)
            {
                _context.TransitionTo<IdleState>();
            }
            else if (nextState == ChargingState.Error)
            {
                _context.TransitionTo<ErrorState>();
            }
            else if(nextState == ChargingState.Initial)
            {
                _context.TransitionTo<InitialState>();
            }
            else
            {
                Console.WriteLine("無效的狀態轉換");
            }
        }
    }


    // Error 狀態
    public class ErrorState : State<ChargingState>
    {
        public ErrorState()
        {
            _stateEnum = ChargingState.Error;
            _currentState = ChargingState.Error;
        }

        public override void EnterState()
        {
            Console.WriteLine("進入 Error 狀態: 發生錯誤");
            SlotServices.StationState = StationState.Error;
        }

        public override void HandleTransition(ChargingState nextState)
        {
            if (nextState == ChargingState.Idle)
            {
                _context.TransitionTo<IdleState>();
            }
            else if(nextState == ChargingState.Initial)
            {
                _context.TransitionTo<InitialState>();
            }
            else
            {
                Console.WriteLine("無效的狀態轉換");
            }
        }
    }

    // 狀態機類別
    public class ChargingStationStateMachine
    {
        public State<ChargingState> _currentState;


        private readonly IServiceProvider _serviceProvider;

        public ChargingStationStateMachine(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeStateMachine();
        }


        private void InitializeStateMachine()
        {
            if (_serviceProvider == null)
            {
                Console.WriteLine("ServiceProvider is null.");
                return;
            }

            try
            {
                _currentState = _serviceProvider.GetRequiredService<InitialState>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve InitializationState: {ex.Message}");
                return;
            }

            _currentState.SetContext(this, _serviceProvider);
            _currentState.EnterState();
        }


        public void TransitionTo<T>() where T : State<ChargingState>
        {
            var newState = _serviceProvider.GetRequiredService<T>();
            Console.WriteLine($"狀態變更: {_currentState?.GetType().Name} -> {newState.GetType().Name}");
            _currentState?.ExitState();
            _currentState = newState;
            _currentState.SetContext(this, _serviceProvider);
            _currentState.EnterState();
        }

        public void HandleTransition(ChargingState nextState)
        {
            _currentState.HandleTransition(nextState);
        }


        public string GetCurrentStateName()
        {
            return _currentState.GetType().Name;
        }

    }
}
