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
        Error
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
                case SlotState.Error:
                    _context.TransitionTo<ErrorSlotState>();
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
        public override void EnterState()
        {
            Console.WriteLine($"Slot[{_index}] 進入未使用狀態");
        }
        public override bool HandleTransition(SlotState nextState)
        {
            bool result = true;
            switch (nextState)
            {
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
        public override bool HandleTransition(SlotState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class IdleSlotState : SlotStateBase<SlotState>
    {
        public override bool HandleTransition(SlotState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class ChargingSlotState : SlotStateBase<SlotState>
    {
        public override bool HandleTransition(SlotState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class FloatingSlotState : SlotStateBase<SlotState>
    {
        public override bool HandleTransition(SlotState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorSlotState : SlotStateBase<SlotState>
    {
        public override bool HandleTransition(SlotState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class SlotStateMachine
    {
        public SlotStateBase<SlotState> _currentState;

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
                _currentState = _serviceProvider.GetRequiredService<InitializationSlotState[]>()[_index];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve InitializationSlotState: {ex.Message}");
                return;
            }


            _currentState.SetContext(this, _serviceProvider,_index);
            _currentState.EnterState();

            
        }


        public void TransitionTo<T>() where T : SlotStateBase<SlotState>
        {
            var newState = _serviceProvider.GetRequiredService<T[]>();
            var newStateInstance = newState[_index];
            Console.WriteLine($"Slot[{_index}] 狀態變更: {_currentState?.GetType().Name} -> {newStateInstance.GetType().Name}");
            _currentState?.ExitState();
            _currentState = newStateInstance;
            _currentState.SetContext(this, _serviceProvider, _index);
            _currentState.EnterState();
        }


        public string GetCurrentStateName()
        {
            return _currentState.GetType().Name;
        }
    }
}
