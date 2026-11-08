using System;

namespace NiaIsland.Fishing
{
    public enum FishingState
    {
        Ready = 0,
        Cast = 1,
        Waiting = 2,
        BiteWindow = 3,
        Miss = 4,
        Resolved = 5
    }

    /// <summary>
    /// Seconds for each automatic transition. Fields are mutable so Play Mode
    /// Inspector edits apply to the next tick.
    /// </summary>
    public sealed class FishingTiming
    {
        public float CastDuration;
        public float WaitingDuration;
        public float BiteWindowDuration;
        public float ResultHoldDuration;

        public FishingTiming(
            float castDuration,
            float waitingDuration,
            float biteWindowDuration,
            float resultHoldDuration)
        {
            CastDuration = castDuration;
            WaitingDuration = waitingDuration;
            BiteWindowDuration = biteWindowDuration;
            ResultHoldDuration = resultHoldDuration;
        }
    }

    /// <summary>
    /// Ready → Cast → Waiting → BiteWindow, then Miss or Resolved, then Ready.
    /// A button press only acts in Ready (start cast) and BiteWindow (hook).
    /// Fish stats are not part of this machine.
    /// </summary>
    public sealed class FishingStateMachine
    {
        readonly FishingTiming _timing;
        float _timeInState;

        public FishingStateMachine(FishingTiming timing)
        {
            if (timing == null)
                throw new ArgumentNullException("timing");
            _timing = timing;
        }

        public FishingState State { get; private set; }

        public float TimeInState
        {
            get { return _timeInState; }
        }

        public float NormalizedTime
        {
            get
            {
                var duration = CurrentDuration;
                if (duration <= 0f)
                    return 1f;
                var t = _timeInState / duration;
                if (t < 0f)
                    return 0f;
                if (t > 1f)
                    return 1f;
                return t;
            }
        }

        public event Action<FishingState> Entered;

        public bool PressFishButton()
        {
            if (State == FishingState.Ready)
            {
                ChangeTo(FishingState.Cast);
                return true;
            }

            if (State == FishingState.BiteWindow)
            {
                ChangeTo(FishingState.Resolved);
                return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;

            var remaining = deltaTime;
            for (var i = 0; i < 8; i++)
            {
                var duration = CurrentDuration;
                if (duration <= 0f)
                {
                    _timeInState += remaining;
                    return;
                }

                var left = duration - _timeInState;
                if (left < 0f)
                    left = 0f;

                if (remaining < left)
                {
                    _timeInState += remaining;
                    return;
                }

                remaining -= left;
                if (!AdvanceByTimeout())
                    return;
            }
        }

        float CurrentDuration
        {
            get
            {
                switch (State)
                {
                    case FishingState.Cast:
                        return _timing.CastDuration;
                    case FishingState.Waiting:
                        return _timing.WaitingDuration;
                    case FishingState.BiteWindow:
                        return _timing.BiteWindowDuration;
                    case FishingState.Miss:
                    case FishingState.Resolved:
                        return _timing.ResultHoldDuration;
                    default:
                        return 0f;
                }
            }
        }

        bool AdvanceByTimeout()
        {
            switch (State)
            {
                case FishingState.Cast:
                    ChangeTo(FishingState.Waiting);
                    return true;
                case FishingState.Waiting:
                    ChangeTo(FishingState.BiteWindow);
                    return true;
                case FishingState.BiteWindow:
                    ChangeTo(FishingState.Miss);
                    return true;
                case FishingState.Miss:
                case FishingState.Resolved:
                    ChangeTo(FishingState.Ready);
                    return true;
                default:
                    return false;
            }
        }

        void ChangeTo(FishingState next)
        {
            State = next;
            _timeInState = 0f;
            var entered = Entered;
            if (entered != null)
                entered(next);
        }
    }
}
