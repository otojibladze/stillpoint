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

The tray icon is your stone's face: eyes open while it counts, sleepy
after a long sit, asleep when it is not counting.


HOW IT WORKS
------------
Press Start when you sit down and I GOT UP when you leave. You can also
right-click the tray icon > Start counting / Stop counting.

Every hour (1 hour, 2 hours, 3 hours...) a small window pops up in the
bottom corner of the screen. It doesn't take over your keyboard, so you
can keep typing or playing. Click KEEP SITTING to close it and keep
counting, or I GOT UP to stop counting.
The gear button next to History opens Settings. There you can change
how often the reminder comes (- and +), pick its sound (or No sound,
the round button plays it) and click "Preview reminder" to see it.
Under Stone you can rename it, make it quieter or chattier, turn on
its sounds (a chime when it hops, a purr when you pet it, crickets at
night) and turn on the desktop buddy: a tiny stone that sits on top of
the taskbar. Click the buddy to open Stillpoint, right-click it to hide
it. It hides by itself while a full-screen game or video is running.

THE STONE
---------
A small stone lives in the sand garden inside the ring.
- While you sit it slowly gets sleepy: its eyes droop, its sprout wilts
  and near the reminder it nods off.
- When you press I GOT UP it stretches and hops. Butterflies only visit
  while you are up.
- Every break today rakes a new line in the sand and grows the sprout.
  After four breaks it flowers. The garden starts fresh each morning.
- Its eyes follow your mouse. Move the mouse slowly over it to pet it,
  click to poke it (five quick pokes make it dizzy).
- "Breathe with me" under the ring starts one minute of slow breathing:
  breathe in while the stone grows, out while it shrinks.
- At night (after 10 pm) and while the computer is locked it sleeps
  under the stars, unless you are counting. Sometimes a cat sleeps
  next to it.
- The first time you open the app it asks for the stone's name. Change
  it any time in Settings.

THE GARDEN
----------
- Seasons follow the calendar: snow in winter (and a snowman after four
  breaks), blossoms in spring, fireflies on summer evenings, leaves in
  autumn.
- The weather follows your sitting. Sit long without a break and clouds
  gather, then it starts to rain. A break clears the sky, sometimes
  with a rainbow, and a frog may come out after the rain.
- Visitors: a butterfly or a bird while you are up, a snail that slowly
  crosses the sand when you have sat a long time.
- After I GOT UP the stone shows you a short stretch (neck rolls,
  shoulder shrugs, reaching up, side bends or looking far away).
- Keepsakes stay in the garden for good: moss after 7 days in a row,
  a stone lantern after 50 breaks, a pond after 100, and a baby pebble
  after 30 days.
- History > Garden shows each day's garden, a week at a time, and your
  keepsakes. In History > Calendar, click the little garden next to a
  day to open it.
- The first time you open Stillpoint after 8 pm you get a short card
  about the day: how long you sat, how often you got up, the longest
  stretch and who visited.

IF YOU WALK AWAY
----------------
If nothing moves for 10 minutes while it counts, a small window asks
"Did you get up?". Click I GOT UP and the time you were away is taken
out: the session ends when you left and a new one starts when you came
back. Click I WAS HERE if you were reading or watching something.

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
every minute. album.csv remembers which visitors came on each day.


GOOD TO KNOW
------------
- In a few old games that take over the whole screen, the reminder can
  only be seen after you leave the game.
- If you forget to press I GOT UP, it keeps counting until you lock the
  computer, put it to sleep or shut it down.


UNINSTALL
---------
Untick "Start with Windows" in Settings, right-click the tray icon > Exit, then delete
Stillpoint.exe. Your data is in %LOCALAPPDATA%\Stillpoint if you want to
delete that too.

