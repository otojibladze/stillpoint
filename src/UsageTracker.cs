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
        public const double SplitPauseSeconds = 600;

        readonly ISystemSignals signals;
        readonly List<Session> sessions;
        Session current;
        Session last;
        uint lastSeenInput;
        double reminderInterval = 3600;
        long remindersShown;
        DateTime pausedAt;

        public UsageTracker(ISystemSignals signals, List<Session> sessions)
        {
            this.signals = signals;
            this.sessions = sessions;
            last = sessions.OrderBy(s => s.Start).LastOrDefault();
            lastSeenInput = signals.LastInputTick();
        }

        public bool Running => current != null;
        public bool CanResume => current == null && last != null && last.End.Date == DateTime.Today;
        public double SecondsAtInput { get; private set; }
        public Session Current => current;
        public const double SettleSeconds = 120;
        public Session Unsettled => current ?? (last != null && (DateTime.Now - last.End).TotalSeconds < SettleSeconds ? last : null);
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
                SecondsAtInput = current?.Seconds ?? 0;
                if ((Paused & PauseReason.Asleep) != 0)
                {
                    Paused &= ~PauseReason.Asleep;
                    if (Paused == PauseReason.None) SplitAfterPause(now);
                }
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
                SecondsAtInput = current.Seconds;
            }
            else if (running && current == null)
            {
                current = new Session { Start = now, End = now };
                sessions.Add(current);
                last = current;
                remindersShown = 0;
                SecondsAtInput = 0;
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

        public double IdleSeconds
        {
            get
            {
                uint input = signals.LastInputTick();
                uint idle = unchecked((uint)Environment.TickCount - input);
                return idle > int.MaxValue ? 0 : idle / 1000.0;
            }
        }

        public void SplitAway(DateTime leftAt, DateTime backAt, double secondsAtLeave, DateTime now)
        {
            if (current == null) return;
            var away = current;
            double counted = away.Seconds;
            away.Seconds = Math.Max(0, Math.Min(counted, secondsAtLeave));
            away.End = leftAt < away.Start ? away.Start : leftAt;
            if (backAt < leftAt) backAt = leftAt;
            if (backAt > now) backAt = now;
            double since = Math.Max(0, Math.Min((now - backAt).TotalSeconds, counted - away.Seconds));
            if (away.Seconds < MinSessionSeconds) sessions.Remove(away);
            current = new Session { Start = backAt, End = now, Seconds = since };
            sessions.Add(current);
            last = current;
            remindersShown = MarksReached();
            Dirty = true;
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
            if (Paused == PauseReason.None)
            {
                Tick(now, elapsedSeconds);
                pausedAt = now;
            }
            Paused |= reason;
            State = TrackState.Paused;
        }

        public void Resume(PauseReason reason, DateTime now)
        {
            bool was = Paused != PauseReason.None;
            Paused &= ~reason;
            lastSeenInput = signals.LastInputTick();
            if (was && Paused == PauseReason.None) SplitAfterPause(now);
        }

        void SplitAfterPause(DateTime now)
        {
            if (current == null || pausedAt == default(DateTime)) return;
            if ((now - pausedAt).TotalSeconds < SplitPauseSeconds && pausedAt.Date == now.Date) return;
            if (current.Seconds < MinSessionSeconds) sessions.Remove(current);
            current = new Session { Start = now, End = now };
            sessions.Add(current);
            last = current;
            remindersShown = 0;
            SecondsAtInput = 0;
            Dirty = true;
        }

        long MarksReached() =>
            current != null && reminderInterval > 0 ? (long)(current.Seconds / reminderInterval) : 0;
    }
}
