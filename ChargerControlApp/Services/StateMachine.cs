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
        Initialization,
        Idle,
        Reserved,
        ReservationTimeout,
        Occupied,
        Charging,
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

    public class InitializationState : State<ChargingStationStateMachine.ChargingState>
    {
        int bypassCounter = 0;
        private readonly GrpcClientService _grpcClientService;  // 已通過構造函數注入
        private readonly AppSettings _settings;

        public InitializationState(GrpcClientService grpcClientService)
        {
            _grpcClientService = grpcClientService;
            ConfigLoader.Load();
            _settings = ConfigLoader.GetSettings();
            _settings = _settings ?? new AppSettings();
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
                    devicePostRegistrationResponse = await _grpcClientService.RegisterDeviceAsync();
                    Console.WriteLine($"收到註冊響應: {devicePostRegistrationResponse.DeviceName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"註冊過程中發生錯誤: {ex.Message}");
                    return; // 錯誤時退出
                }

                await Task.Delay(5000); // 進行輪詢
            } while (devicePostRegistrationResponse.DeviceName != _settings.ChargingStationName);

            // 註冊成功後，轉換到 Idle 狀態
            _context.TransitionTo<IdleState>();
        }

        public override void HandleTransition(ChargingState nextState) { }
    }


    // Idle 狀態
    public class IdleState : State<ChargingStationStateMachine.ChargingState>
    {
        public override void EnterState()
        {
            Console.WriteLine("進入 Idle 狀態: 等待預約或車輛進入");
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Reserved:
                    _context.TransitionTo<ReservedState>();
                    break;
                case ChargingState.Occupied:
                    _context.TransitionTo<OccupiedState>();
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

    // Reserved 狀態
    public class ReservedState : State<ChargingStationStateMachine.ChargingState>
    {
        public override void EnterState()
        {
            Console.WriteLine("進入 Reserved 狀態: FMS 預約");
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Occupied:
                    _context.TransitionTo<OccupiedState>();
                    break;
                case ChargingState.ReservationTimeout:
                    _context.TransitionTo<ReservationTimeoutState>();
                    break;
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

    // ReservationTimeout 狀態
    public class ReservationTimeoutState : State<ChargingStationStateMachine.ChargingState>
    {
        public override void EnterState()
        {
            Console.WriteLine("進入 ReservationTimeout 狀態: 回報 FMS 預約超時");
        }

        public override void HandleTransition(ChargingState nextState)
        {
            if (nextState == ChargingState.Idle)
            {
                _context.TransitionTo<IdleState>();
            }
            else if (nextState == ChargingState.Reserved)
            {
                _context.TransitionTo<ReservedState>();
            }
            else
            {
                Console.WriteLine("無效的狀態轉換");
            }
        }
    }

    // Occupied 狀態
    public class OccupiedState : State<ChargingStationStateMachine.ChargingState>
    {
        public override void EnterState()
        {
            Console.WriteLine("進入 Occupied 狀態: 車輛偵測到");
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Charging:
                    _context.TransitionTo<ChargingStateClass>();
                    break;
                case ChargingState.Idle:
                    _context.TransitionTo<IdleState>();
                    break;
                default:
                    Console.WriteLine("無效的狀態轉換");
                    break;
            }
        }
    }

    // Charging 狀態
    public class ChargingStateClass : State<ChargingStationStateMachine.ChargingState>
    {
        public override void EnterState()
        {
            Console.WriteLine("進入 Charging 狀態: 充電中...");
        }

        public override void HandleTransition(ChargingState nextState)
        {
            switch (nextState)
            {
                case ChargingState.Occupied:
                    _context.TransitionTo<OccupiedState>();
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

    // Error 狀態
    public class ErrorState : State<ChargingStationStateMachine.ChargingState>
    {
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
        public State<ChargingStationStateMachine.ChargingState> _currentState;
        public enum ChargingState
        {
            Initialization,
            Idle,
            Reserved,
            Occupied,
            Charging,
            Error,
            ReservationTimeout
        }


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
                _currentState = _serviceProvider.GetRequiredService<InitializationState>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve InitializationState: {ex.Message}");
                return;
            }

            _currentState.SetContext(this, _serviceProvider);
            _currentState.EnterState();
        }


        public void TransitionTo<T>() where T : State<ChargingStationStateMachine.ChargingState>
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
