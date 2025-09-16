using ChargerControlApp.Services;

namespace ChargerControlApp.DataAccess.Slot.Services
{
    public enum SlotState
    {
        Initialization,
        Empty,
        Idle,
        Charging,
        Finished,
        Loading,
        Unloading,
        Error
    }

    public abstract class SlotStateBase<T> where T : Enum
    {
        protected SlotStateMachine _context;
        protected IServiceProvider _serviceProvider;
        protected SlotState _stateEnum;
        protected T _currentState;
        public T CurrentState => _currentState;
        public void SetContext(SlotStateMachine context, IServiceProvider serviceProvider)
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
        public SlotState GetStateEnum() => _stateEnum;
        public bool IsCurrentState(T state) => _currentState.Equals(state);
        public abstract void HandleTransition(ChargingState nextState);
    }

    public class InitializationSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptySlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class IdleSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class ChargingSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class FinishedSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class LoadingSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class UnloadingSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorSlotState : SlotStateBase<SlotState>
    {
        public override void HandleTransition(ChargingState nextState)
        {
            throw new NotImplementedException();
        }
    }

    public class SlotStateMachine
    { 
    
    }
}
