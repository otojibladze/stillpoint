STILLPOINT
==========

Counts how long you sit at your computer and reminds you every hour.


SETUP (one time)
----------------
1. Make a folder that will stay put, for example Documents\Stillpoint,
   and put Stillpoint.exe in it. Don't leave it in Downloads if you clean
   that folder: Windows starts the app from wherever the exe is.
2. Double-click Stillpoint.exe.
   If Windows says "Windows protected your PC", click "More info", then
   "Run anyway". It says that because the app is not signed.
3. The Stillpoint window opens and a clock icon appears in the tray, next
   to the Windows clock. If you don't see it, click the ^ arrow there.
   Drag the icon from there onto the taskbar to always see it.
   From now on it starts by itself when you sign in to Windows.

Closing the window does NOT stop it. It keeps running in the tray.
Click the clock icon to open it again. Right-click the icon > Exit to quit.

The icon is green while it counts and grey while it doesn't.


HOW IT WORKS
------------
Press Start when you sit down and I GOT UP when you leave. You can also
right-click the tray icon > Start counting / Stop counting.

Every hour (1 hour, 2 hours, 3 hours...) a small window pops up in the
bottom corner of the screen. It doesn't take over your keyboard, so you
can keep typing or playing. Click KEEP SITTING to close it and keep
counting, or I GOT UP to stop counting.
The gear button next to History opens Settings. There you can change
"Remind me every 60 minutes", pick the reminder sound (or No sound),
and click "Preview" to see and hear what it looks like.

Each Start begins a new session at 0:00:00. After you stop, Resume
carries on the last session instead, so a short break does not split
it in two (the break itself is not counted).

The History button opens your history. List shows every session grouped
by day, with each day's total. Calendar shows the month: the darker a
day, the longer you sat. Click a day to see its sessions. The arrows (or
the mouse wheel) change the month; Today jumps back.

It pauses while the computer is locked or asleep and carries on when
you come back. If you leave it counting and shut down, it is still
counting the next time you sign in (starting a new session).


UPDATES
-------
Stillpoint checks for a new version a few times a day. When there is
one, a message pops up and the window shows "Update to ...". Click it:
the app downloads the new version and restarts by itself in a couple of
seconds. Your sessions and settings stay, and a running session keeps
counting. You can also click "Check for updates" in Settings any time.


YOUR DATA
---------
Click "Open data folder" in Settings. sessions.csv has one line per
session (start, end, seconds, length) and opens in Excel. It is saved
every minute.


GOOD TO KNOW
------------
- In a few old games that take over the whole screen, the reminder can
  only be seen after you leave the game.
- If you forget to press Stop, it keeps counting until you lock the
  computer, put it to sleep or shut it down.


UNINSTALL
---------
Untick "Start with Windows" in Settings, right-click the tray icon > Exit, then delete
Stillpoint.exe. Your data is in %LOCALAPPDATA%\Stillpoint if you want to
delete that too.

