using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class WindowsSignals : ISystemSignals
    {
        uint lastInput;

        public uint LastInputTick()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (GetLastInputInfo(ref info)) lastInput = info.dwTime;
            return lastInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    }

    sealed class HotKey : NativeWindow, IDisposable
    {
        const int WmHotKey = 0x0312;
        const int Id = 1;
        readonly Action pressed;
        public bool Registered { get; }

        public HotKey(int modifiers, Keys key, Action pressed)
        {
            this.pressed = pressed;
            CreateHandle(new CreateParams());
            Registered = RegisterHotKey(Handle, Id, modifiers | 0x4000, (int)key);
        }

        public const int Alt = 1;
        public const int Control = 2;
        public const int Shift = 4;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotKey && (int)m.WParam == Id) pressed();
            else base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Handle == IntPtr.Zero) return;
            if (Registered) UnregisterHotKey(Handle, Id);
            DestroyHandle();
        }

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, int modifiers, int key);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    }

    static class Native
    {
        const int RestartNoPatch = 4;
        const int RestartNoReboot = 8;

        public static void RegisterRestart(string arguments)
        {
            try
            {
                RegisterApplicationRestart(arguments, RestartNoPatch | RestartNoReboot);
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        public static void AllowAnyForeground() => AllowSetForegroundWindow(-1);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int RegisterApplicationRestart(string commandLine, int flags);

        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int processId);
    }
}
