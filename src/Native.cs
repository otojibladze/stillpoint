using System;
using System.Runtime.InteropServices;

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

        public static DateTime BootTime() => DateTime.Now - TimeSpan.FromMilliseconds(GetTickCount64());

        [DllImport("kernel32.dll")]
        static extern ulong GetTickCount64();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int RegisterApplicationRestart(string commandLine, int flags);

        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int processId);
    }
}
