using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class Frames : IDisposable
    {
        static readonly List<Frames> active = new List<Frames>();
        static bool hooked;

        [StructLayout(LayoutKind.Sequential)]
        struct Msg { public IntPtr Hwnd; public uint Message; public IntPtr W, L; public uint Time; public int X, Y; }

        [DllImport("user32.dll")] static extern bool PeekMessage(out Msg msg, IntPtr hwnd, uint min, uint max, uint remove);
        [DllImport("dwmapi.dll")] static extern int DwmFlush();

        readonly Stopwatch clock = new Stopwatch();
        readonly Func<float, bool> step;

        public Frames(Func<float, bool> step) => this.step = step;

        public static bool Busy => active.Count > 0;

        public static event Action<Exception> Failed;

        public bool Running => active.Contains(this);

        public void Start()
        {
            if (Running) return;
            clock.Restart();
            active.Add(this);
            if (!hooked) { hooked = true; Application.Idle += Loop; }
        }

        public void Stop()
        {
            active.Remove(this);
            if (active.Count == 0 && hooked) { hooked = false; Application.Idle -= Loop; }
        }

        public void Dispose() => Stop();

        static void Loop(object sender, EventArgs e)
        {
            while (active.Count > 0 && !PeekMessage(out _, IntPtr.Zero, 0, 0, 0))
            {
                foreach (var f in active.ToArray())
                {
                    if (!active.Contains(f)) continue;
                    float dt = (float)Math.Min(0.05, f.clock.Elapsed.TotalSeconds);
                    f.clock.Restart();
                    bool more;
                    try { more = f.step(dt); }
                    catch (Exception ex) { more = false; Failed?.Invoke(ex); }
                    if (!more) f.Stop();
                }
                if (active.Count == 0) break;
                int ok;
                try { ok = DwmFlush(); } catch (Exception) { ok = -1; }
                if (ok != 0) Thread.Sleep(10);
            }
        }
    }

    sealed class Glide : IDisposable
    {
        const float Rate = 13;
        readonly Frames frames;
        readonly Action<float> apply;

        public Glide(Action<float> apply)
        {
            this.apply = apply;
            frames = new Frames(Step);
        }

        public float Value { get; private set; }
        public float Target { get; private set; }

        public void Jump(float value)
        {
            frames.Stop();
            Value = Target = value;
            apply(value);
        }

        public void To(float target)
        {
            Target = target;
            if (Math.Abs(Target - Value) < 0.5f) { Jump(target); return; }
            frames.Start();
        }

        bool Step(float dt)
        {
            Value += (Target - Value) * (1 - (float)Math.Exp(-dt * Rate));
            bool done = Math.Abs(Target - Value) < 0.5f;
            if (done) Value = Target;
            apply(Value);
            return !done;
        }

        public void Dispose() => frames.Dispose();
    }
}
