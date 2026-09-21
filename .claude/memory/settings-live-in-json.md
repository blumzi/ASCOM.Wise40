---
name: settings-live-in-json
description: "Driver settings live in c:\\Wise40\\settings.json since 2026-09-21, not the ASCOM Profile registry - registration still lives in the registry, and the file survives the rebuild that used to wipe values"
metadata:
  type: project
---

**Since 2026-09-21, driver settings are in `c:\Wise40\settings.json`, not the ASCOM Profile.**

`Common/WiseProfile.cs` is a drop-in for `ASCOM.Utilities.Profile` backed by that file. Read its
header comment before changing anything here; this note is the operational summary.

## Why

A build deleted them. `RegisterForComInterop` makes regasm run each driver's
`[ComUnregisterFunction]`, which calls `Profile.Unregister`, which deletes the **entire device
profile tree**. `ReadProfile` then re-created the settings from their **code defaults**, so
everything looked present and correct while holding whatever the source said rather than what the
observatory was set to. That is how a deliberate switch to `EncodersInUse = New` was lost.

The ASCOM Initiative reached the same conclusion: Platform 6.6 SP1 removed the COM registration
functions from the driver templates, and the modern ASCOM Library ships `ASCOM.Tools.XMLProfile`,
which stores settings in a file.

## What moved and what did not

- **Values moved.** 129 of them, 8 sections.
- **Registration did NOT.** `Profile.Register` / `Unregister` / `IsRegistered` still go to the
  real ASCOM Profile, because that is what puts a driver in the Chooser. An elevated rebuild
  still wipes the registry tree - it just no longer costs you any settings.
- **ObservatoryMonitor** had been registered under two device trees and its settings landed in
  whichever one the caller named. It now has exactly one section, with aliases folding the old
  ProgIDs into it.

## Things that will bite

**Values are real JSON types**, not strings - 61 ints, 19 bools, 49 strings. The conversion is
round-trip guarded, so RFIDs (`"7F001B4C16"`), IP addresses and `COM1` stay strings; only values
that format back identically became numbers. Reads still hand drivers the string form they
expect (`"True"`/`"False"`), which is safe because all 22 boolean reads use `Convert.ToBoolean`
or `bool.TryParse`.

**The root sub-key is named `(root)`, not `""`.** JSON permits an empty property name and
Newtonsoft reads it, but **PowerShell 5.1 refuses the whole document** with
`ConvertFrom-Json : Cannot process argument because the value of argument "name" is not valid`.
One unreadable property name poisons the entire file for scripting.

**A setting changed by another process is picked up.** `Load()` compares the file's last write
time and re-reads when it moves. This matters for the ordinary setup flow: an inproc COM driver's
setup dialog runs in the **caller's** process (the Chooser, or whichever hub opened Properties),
never in the RemoteServer process where the live driver instance sits. Without it a changed
setting would silently not take effect until a restart. Verified with two real processes, because
every single-process test passed while the gap was there.

**The cross-process lock needs its DACL.** See [[global-mutex-across-accounts]] - that fault
killed both ObservingConditions drivers before it was found.

## Operating it

The file is meant to be read and edited by hand. Back it up before a deploy:

```
copy C:\Wise40\settings.json C:\Wise40\settings.json.predeploy
```

If it is ever lost, `WiseProfile` self-seeds from whatever the ASCOM Profile registry still
holds - so keep taking `reg export "HKLM\SOFTWARE\WOW6432Node\ASCOM"` backups too, since that
registry tree is now a *fallback* rather than the source of truth.
