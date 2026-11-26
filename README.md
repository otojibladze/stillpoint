# Pebble

A small Windows tray app for people who want to sit less. A little stone lives in a sand garden, gets sleepy while you sit and reminds you to get up. Formerly called Stillpoint.
**Download:** [latest/Pebble.exe](https://github.com/otojibladze/stillpoint/raw/main/latest/Pebble.exe) (Windows 10/11, nothing to install). How to use it: [README.txt](README.txt).

## Build and publish

Needs Visual Studio Build Tools (C# compiler) on Windows.

```powershell
.\build.ps1                      # builds dist\Stillpoint.exe
.\publish.ps1 -Version 1.0.1     # sets the version, builds, copies to latest\ (Stillpoint.exe and Pebble.exe), commits, pushes
```

Installed copies check `latest/version.txt` and update themselves from `latest/Stillpoint.exe` (the file name stays for older installs).