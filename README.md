# Pebble

A small Windows tray app for people who want to sit less. A little stone lives in a sand garden, gets sleepy while you sit and reminds you to get up. Formerly called Stillpoint.
**Download:** [latest/Pebble.exe](https://github.com/otojibladze/stillpoint/raw/main/latest/Pebble.exe) (Windows 10/11, nothing to install). How to use it: [README.txt](README.txt).

## Build and publish

Needs Visual Studio Build Tools (C# compiler) on Windows.

```powershell
.\build.ps1                      # builds dist\Pebble.exe
.\publish.ps1 -Version 1.0.1     # sets the version, builds, copies to latest\ (Stillpoint.exe and Pebble.exe), commits, pushes
```

Installed copies check `latest/version.txt` and update themselves from `latest/Pebble.exe` (installs older than 3.3.1 use `latest/Stillpoint.exe`, kept as a copy).