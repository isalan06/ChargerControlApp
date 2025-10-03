using ChargerControlApp.Hardware;
using ChargerControlApp.Services;
using ChargerControlApp.Utilities;
using Grpc.Core;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TacDynamics.Kernel.DeviceService.Protos;

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
        private readonly HardwareManager _hardwareManager;

        //public InitializationState(GrpcClientService grpcClientService)
        //{
        //    _grpcClientService = grpcClientService;
        //    ConfigLoader.Load();
        //    _settings = ConfigLoader.GetSettings();
        //    _settings = _settings ?? new AppSettings();
        //}
        public InitialState()
        {
            _stateEnum = ChargingState.Initial;
            _currentState = ChargingState.Initial;
        }

        public override void EnterState()
        {
            Console.WriteLine("進入 Initialization 狀態: 檢查設定，連接 WiFi，FMS 註冊");

            // 將非同步操作移到外部方法中
            InitializeAsync();
        }

        private async Task InitializeAsync()  // 改為返回 Task，方便處理異常
        {
            string responseDeviceName = "";
            await Task.Delay(5000); // 初步延遲

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
                    return; // 錯誤時退出
                }

                await Task.Delay(5000); // 進行輪詢
            } while (false);//devicePostRegistrationResponse.DeviceName != _settings.ChargingStationName);

            // ToDo : 自動ServoOn

            // ToDo : 執行馬達原點復歸

            // 註冊成功後，轉換到 Idle 狀態
            _context.TransitionTo<IdleState>();
        }

        public override void HandleTransition(ChargingState nextState) { }
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
        }

        public override void HandleTransition(ChargingState nextState)
        {
            if (nextState == ChargingState.Idle)
            {
                _context.TransitionTo<IdleState>();
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


        public string GetCurrentStateName()
        {
            return _currentState.GetType().Name;
        }

    }
}
