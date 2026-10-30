using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DoctorHelp
{
    public enum DayStep { Preparation = 0, Examination = 10, TreatmentDecision = 20, PatientDeparture = 25, DayResults = 30 }

    public interface IDayFlowState
    {
        DayStep Id { get; }
        Task EnterAsync(CancellationToken cancellation);
        void Exit();
    }

    // Order is explicit; enum numeric order never controls gameplay.
    public sealed class DayFlowPlan
    {
        private readonly DayStep[] steps;
        private DayStep repeatFrom, repeatTo;
        private Func<bool> shouldRepeat;
        public DayStep Initial => steps[0];
        public IReadOnlyList<DayStep> Steps => Array.AsReadOnly(steps);

        public DayFlowPlan(params DayStep[] sequence)
        {
            if (sequence == null || sequence.Length < 2 || sequence[0] != DayStep.Preparation ||
                sequence[sequence.Length - 1] != DayStep.DayResults)
                throw new ArgumentException("A day must start with Preparation and end with DayResults.");
            steps = (DayStep[])sequence.Clone();
            if (new HashSet<DayStep>(steps).Count != steps.Length)
                throw new ArgumentException("A flow cannot contain duplicate steps.");
        }

        public void RepeatWhile(DayStep from, DayStep to, Func<bool> condition)
        {
            int start = Array.IndexOf(steps, to), end = Array.IndexOf(steps, from);
            if (start <= 0 || end < start || end >= steps.Length - 1 || condition == null)
                throw new ArgumentException("A repeat must stay between Preparation and DayResults.");
            repeatFrom = from;
            repeatTo = to;
            shouldRepeat = condition;
        }

        public DayStep Next(DayStep current)
        {
            int index = Array.IndexOf(steps, current);
            if (index < 0) throw new ArgumentException("Step is not in this flow: " + current);
            if (shouldRepeat != null && current == repeatFrom && shouldRepeat()) return repeatTo;
            return steps[(index + 1) % steps.Length];
        }
    }

    // Pure C#: serializes transitions and owns state entry/exit, fault and retry semantics.
    public sealed class DayFlowMachine
    {
        private readonly DayFlowPlan plan;
        private readonly Dictionary<DayStep, IDayFlowState> states = new Dictionary<DayStep, IDayFlowState>();
        private IDayFlowState active;
        private bool stopped;
        public DayStep? CurrentStep { get; private set; }
        public DayStep? FailedStep { get; private set; }
        public Exception LastError { get; private set; }
        public bool IsTransitioning { get; private set; }

        public DayFlowMachine(DayFlowPlan plan, params IDayFlowState[] registeredStates)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            foreach (var state in registeredStates)
            {
                if (state == null || states.ContainsKey(state.Id))
                    throw new ArgumentException("State registrations must be non-null and unique.");
                states.Add(state.Id, state);
            }
            foreach (var step in plan.Steps)
                if (!states.ContainsKey(step)) throw new ArgumentException("Unregistered step: " + step);
        }

        public Task<bool> StartAsync(CancellationToken token) =>
            CurrentStep == null && FailedStep == null ? TransitionAsync(plan.Initial, token) : Task.FromResult(false);

        public Task<bool> AdvanceAsync(CancellationToken token) =>
            CurrentStep.HasValue && !FailedStep.HasValue
                ? TransitionAsync(plan.Next(CurrentStep.Value), token) : Task.FromResult(false);

        public Task<bool> RetryAsync(CancellationToken token) =>
            FailedStep.HasValue ? TransitionAsync(FailedStep.Value, token) : Task.FromResult(false);

        private async Task<bool> TransitionAsync(DayStep target, CancellationToken token)
        {
            if (stopped || IsTransitioning || token.IsCancellationRequested) return false;
            IsTransitioning = true;
            var next = states[target];
            try
            {
                var previous = active;
                active = null;
                previous?.Exit();
                token.ThrowIfCancellationRequested();
                await next.EnterAsync(token);
                token.ThrowIfCancellationRequested();
                if (stopped) throw new OperationCanceledException();
                active = next;
                CurrentStep = target;
                FailedStep = null;
                LastError = null;
                return true;
            }
            catch (Exception error)
            {
                try { next.Exit(); }
                catch (Exception cleanupError) { error = new AggregateException(error, cleanupError); }
                FailedStep = target;
                LastError = error;
                return false;
            }
            finally { IsTransitioning = false; }
        }

        public void Stop()
        {
            stopped = true;
            var previous = active;
            active = null;
            previous?.Exit();
        }
    }
}
