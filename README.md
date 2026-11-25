# Stillpoint

A small Windows tray app for people who want to sit less. Press Start when you sit down; every hour a reminder pops up. Stopping it, or closing a reminder, takes a small random puzzle, so it is a choice and not a reflex.

**Download:** [latest/Stillpoint.exe](https://github.com/otojibladze/stillpoint/raw/main/latest/Stillpoint.exe) (Windows 10/11, nothing to install). How to use it: [README.txt](README.txt).

## Build and publish

Needs Visual Studio Build Tools (C# compiler) on Windows.

```powershell
.\build.ps1                      # builds dist\Stillpoint.exe
.\publish.ps1 -Version 1.0.1     # sets the version, builds, copies to latest\, commits, pushes
```

Installed copies check `latest/version.txt` and update themselves from `latest/Stillpoint.exe`.
