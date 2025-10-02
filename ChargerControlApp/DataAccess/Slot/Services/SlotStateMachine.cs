using ChargerControlApp.Services;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public enum SlotState
    {
        Initialization,
        NotUsed,
        Empty,
        Idle,
        Charging,
        Floating,
        StopCharge,
        SupplyError,
        StateError
    }

    public abstract class SlotStateBase<T> where T : Enum
    {
        protected SlotStateMachine _context;
        protected IServiceProvider _serviceProvider;
        protected SlotState _stateEnum;
        protected T _currentState;
        protected int _index = 0;
        public T CurrentState => _currentState;
        public void SetContext(SlotStateMachine context, IServiceProvider serviceProvider, int index)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _index = index;
        }
        public virtual void TransistTo(T newState)
        {
            Console.WriteLine($"Slot[{_index}] 狀態轉換: {_currentState} → {newState}");
            _currentState = newState;
        }
        public virtual void EnterState() { }
        public virtual void ExitState() { }
        public T GetCurrentStete() => _currentState;
        public SlotState GetStateEnum() => _stateEnum;
        public bool IsCurrentState(T state) => _currentState.Equals(state);
        public abstract bool HandleTransition(SlotState nextState);
    }

    public class InitializationSlotState : SlotStateBase<SlotState>
    {
        public InitializationSlotState()
        {
            _currentState = _stateEnum = SlotState.Initialization;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入初始化狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Empty:
                    _context.TransitionTo<EmptySlotState>();
                    break;
                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;
                case SlotState.Idle:
                    _context.TransitionTo<IdleSlotState>();
                    break;
                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;
                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;
                case SlotState.Initialization:
                    Console.WriteLine($"Slot[{_index}] 已經在初始化狀態");
                    break;
                case SlotState.Charging:
                    _context.TransitionTo<ChargingSlotState>();
                    break;
                case SlotState.Floating:
                    _context.TransitionTo<FloatingSlotState>();
                    break;
                case SlotState.StopCharge:
                    _context.TransitionTo<StopChargeSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }
    public class NotUsedSlotState : SlotStateBase<SlotState>
    {
        public NotUsedSlotState()
        {
            _currentState = _stateEnum = SlotState.NotUsed;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入未使用狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.NotUsed:
                    Console.WriteLine($"Slot[{_index}] 已經在未使用狀態");
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class EmptySlotState : SlotStateBase<SlotState>
    {
        public EmptySlotState()
        {
            _currentState = _stateEnum = SlotState.Empty;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入空狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.Empty:
                    Console.WriteLine($"Slot[{_index}] 已經在空狀態");
                    break;

                case SlotState.Idle:
                    _context.TransitionTo<IdleSlotState>();
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;

                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class IdleSlotState : SlotStateBase<SlotState>
    {
        public IdleSlotState()
        {
            _currentState = _stateEnum = SlotState.Idle;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入閒置狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.Idle:
                    Console.WriteLine($"Slot[{_index}] 已經在閒置狀態");
                    break;

                case SlotState.Charging:
                    _context.TransitionTo<ChargingSlotState>();
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;

                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class ChargingSlotState : SlotStateBase<SlotState>
    {
        public ChargingSlotState()
        {
            _currentState = _stateEnum = SlotState.Charging;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入充電狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.Charging:
                    Console.WriteLine($"Slot[{_index}] 已經在充電狀態");
                    break;

                case SlotState.Floating:
                    _context.TransitionTo<FloatingSlotState>();
                    break;

                case SlotState.StopCharge:
                    _context.TransitionTo<StopChargeSlotState>();
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;

                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class FloatingSlotState : SlotStateBase<SlotState>
    {
        public FloatingSlotState()
        {
            _currentState = _stateEnum = SlotState.Floating;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入浮充狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.Floating:
                    Console.WriteLine($"Slot[{_index}] 已經在浮充狀態");
                    break;

                case SlotState.Charging:
                    _context.TransitionTo<ChargingSlotState>();
                    break;

                case SlotState.StopCharge:
                    _context.TransitionTo<StopChargeSlotState>();
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;

                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class StopChargeSlotState : SlotStateBase<SlotState>
    {
        public StopChargeSlotState()
        {
            _currentState = _stateEnum = SlotState.StopCharge;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入停止充電狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.StopCharge:
                    Console.WriteLine($"Slot[{_index}] 已經在停止充電狀態");
                    break;

                case SlotState.Charging:
                    _context.TransitionTo<ChargingSlotState>();
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                case SlotState.SupplyError:
                    _context.TransitionTo<SupplyErrorSlotState>();
                    break;

                case SlotState.StateError:
                    _context.TransitionTo<StateErrorSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class SupplyErrorSlotState : SlotStateBase<SlotState>
    {
        public SupplyErrorSlotState()
        {
            _currentState = _stateEnum = SlotState.SupplyError;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入電源供應器錯誤狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.SupplyError:
                    Console.WriteLine($"Slot[{_index}] 已經在電源供應器錯誤狀態");
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class StateErrorSlotState : SlotStateBase<SlotState>
    {
        public StateErrorSlotState()
        {
            _currentState = _stateEnum = SlotState.StateError;
        }
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入狀態錯誤狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
                case SlotState.Initialization:
                    _context.TransitionTo<InitializationSlotState>();
                    break;

                case SlotState.StateError:
                    Console.WriteLine($"Slot[{_index}] 已經在狀態錯誤狀態");
                    break;

                case SlotState.NotUsed:
                    _context.TransitionTo<NotUsedSlotState>();
                    break;

                default:
                    Console.WriteLine($"Slot[{_index}] 無效的狀態轉換:  {_currentState} → {nextState}");
                    result = false;
                    break;
            }

            return result;
        }
    }

    public class SlotStateMachine
    {
        public SlotStateBase<SlotState> CurrentState;

        private readonly IServiceProvider _serviceProvider;
        private int _index = 0;

        public SlotStateMachine(IServiceProvider serviceProvider, int index)
        {
            _serviceProvider = serviceProvider;
            _index = index;
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
                CurrentState = _serviceProvider.GetRequiredService<InitializationSlotState[]>()[_index];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve InitializationSlotState: {ex.Message}");
                return;
            }


            CurrentState.SetContext(this, _serviceProvider,_index);
            CurrentState.EnterState();

            
        }


        public void TransitionTo<T>() where T : SlotStateBase<SlotState>
        {
            var newState = _serviceProvider.GetRequiredService<T[]>();
            var newStateInstance = newState[_index];
            Console.WriteLine($"Slot[{_index}] 狀態變更: {CurrentState?.GetType().Name} -> {newStateInstance.GetType().Name}");
            CurrentState?.ExitState();
            CurrentState = newStateInstance;
            CurrentState.SetContext(this, _serviceProvider, _index);
            CurrentState.EnterState();
        }


        public string GetCurrentStateName()
        {
            return CurrentState.GetType().Name;
        }

        public void TransitionToState(SlotState state)
        {
            CurrentState.HandleTransition(state);
        }
    }
}
