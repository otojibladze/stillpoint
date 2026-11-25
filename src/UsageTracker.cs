using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    public interface ISystemSignals
    {
        uint LastInputTick();
    }

    public enum TrackState { Counting, Stopped, Paused }

    [Flags]
    public enum PauseReason { None = 0, Locked = 1, Asleep = 2 }

    public sealed class Session
    {
        public DateTime Start;
        public DateTime End;
        public double Seconds;
    }

    public sealed class UsageTracker
    {
        public const double MaxTickSeconds = 10;
        public const double MinSessionSeconds = 1;

        readonly ISystemSignals signals;
        readonly List<Session> sessions;
        Session current;
        Session last;
        uint lastSeenInput;
        double reminderInterval = 3600;
        long remindersShown;

        public UsageTracker(ISystemSignals signals, List<Session> sessions)
        {
            this.signals = signals;
            this.sessions = sessions;
            last = sessions.OrderBy(s => s.Start).LastOrDefault();
            lastSeenInput = signals.LastInputTick();
        }

        public bool Running => current != null;
        public Session Current => current;
        public DateTime SessionStarted => (current ?? last)?.Start ?? default(DateTime);
        public double SessionSeconds => (current ?? last)?.Seconds ?? 0;
        public PauseReason Paused { get; private set; }
        public TrackState State { get; private set; } = TrackState.Stopped;
        public bool Dirty { get; set; }
        public IReadOnlyList<Session> Sessions => sessions;
        public bool IsCounting => State == TrackState.Counting;

        public double ReminderIntervalSeconds
        {
            get => reminderInterval;
            set
            {
                reminderInterval = value;
                remindersShown = MarksReached();
            }
        }

        public void Tick(DateTime now, double elapsedSeconds)
        {
            double delta = elapsedSeconds > 0 && elapsedSeconds <= MaxTickSeconds ? elapsedSeconds : 0;
            uint input = signals.LastInputTick();
            if (input != lastSeenInput)
            {
                lastSeenInput = input;
                Paused &= ~PauseReason.Asleep;
            }
            if (Paused != PauseReason.None)
            {
                State = TrackState.Paused;
                return;
            }
            if (current == null)
            {
                State = TrackState.Stopped;
                return;
            }
            if (delta > 0)
            {
                current.Seconds += delta;
                current.End = now;
                Dirty = true;
            }
            State = TrackState.Counting;
        }

        public void SetRunning(bool running, DateTime now, bool continueLast = false)
        {
            if (running && current == null && continueLast && last != null)
            {
                current = last;
                remindersShown = MarksReached();
            }
            else if (running && current == null)
            {
                current = new Session { Start = now, End = now };
                sessions.Add(current);
                last = current;
                remindersShown = 0;
                Dirty = true;
            }
            else if (!running && current != null)
            {
                if (current.Seconds < MinSessionSeconds)
                {
                    sessions.Remove(current);
                    last = sessions.OrderBy(s => s.Start).LastOrDefault();
                }
                current = null;
                Dirty = true;
            }
        }

        public bool TryTakeReminder(out double sessionMark)
        {
            long reached = MarksReached();
            sessionMark = reached * reminderInterval;
            if (reached <= remindersShown) return false;
            remindersShown = reached;
            return true;
        }

        public void Pause(PauseReason reason, DateTime now, double elapsedSeconds)
        {
            if (Paused == PauseReason.None) Tick(now, elapsedSeconds);
            Paused |= reason;
            State = TrackState.Paused;
        }

        public void Resume(PauseReason reason)
        {
            Paused &= ~reason;
            lastSeenInput = signals.LastInputTick();
        }

        long MarksReached() =>
            current != null && reminderInterval > 0 ? (long)(current.Seconds / reminderInterval) : 0;
    }
}
