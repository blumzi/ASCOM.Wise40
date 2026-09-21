---
name: no-safe-unelevated-solution-build
description: "There is no safe way to build this solution unelevated - /t:Build, /t:Compile and /p:RegisterForComInterop=false all reach regasm, and the false setting actively UNREGISTERS drivers"
metadata:
  type: reference
---

**Do not build this solution locally while unelevated. There is no safe switch.** Several
projects set `RegisterForComInterop`, so MSBuild runs regasm, and every route reaches it:

| attempt | what actually happens |
|---|---|
| `/t:Build` | registers - unelevated it unregisters first and then fails to re-register |
| `/t:Compile` | **still reaches the register target** (MSB3216 at `Microsoft.Common.CurrentVersion.targets(5384,5)`) |
| `/p:RegisterForComInterop=false` | **runs `UnregisterAssembly`** - it does not mean "skip", it means "make sure it is unregistered" |

## The one that did damage

On 2026-09-21 a full solution build with `/p:RegisterForComInterop=false` - chosen *specifically*
to keep regasm out - deleted `ASCOM.Wise40.TessW.ObservingConditions` from the ASCOM Profile.

The build log showed only this, which reads like nothing happened:

```
error MSB3392: Cannot unregister assembly "...TessW...dll" - access denied.
  Please make sure you're running the application as administrator.
```

**The access-denied is only half the story.** Unregistration has two parts, and they have
different permissions:

1. the driver's `[ComUnregisterFunction]` → `Profile.Unregister(driverID)`, which deletes the
   entire ASCOM profile tree for that driver. The ASCOM profile tree under
   `HKLM\SOFTWARE\WOW6432Node\ASCOM` is **deliberately writable by non-admins** (that is how a
   driver saves settings without elevation), so this half **SUCCEEDS unelevated**.
2. the CLSID/ProgID entries under `HKEY_CLASSES_ROOT`, which **need admin** and fail.

So the error message reports the harmless half failing while the damaging half has already
completed. **A failed unregister is not a safe unregister.**

## Detecting and repairing it

Count the drivers - a silent drop is the symptom:

```powershell
Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\ASCOM\ObservingConditions Drivers" |
    Where-Object { $_.PSChildName -match 'Wise' }
```

Repair needs no elevation, for the same ACL reason that caused the problem: extract the driver's
key from a `reg export` backup and `reg import` it. It restores values too - TessW's key carries
`IPAddress` and `Enabled`, which would otherwise come back as code defaults.

**So take the backup before any build:**

```
reg export "HKLM\SOFTWARE\WOW6432Node\ASCOM" C:\Wise40\ascom-profile-backup.reg
```

## What to do instead

Verify compilation **through the elevated deploy** (`tools/deploy.ps1`), which registers properly
as part of its job. If a compile-only answer is needed unelevated, build a **single project that
has no `RegisterForComInterop`**, never the solution.

This is the same family as [[wise40-build-and-environment-gotchas]] - an elevated rebuild wipes
ASCOM Profile *values*; this wipes the *registration*. Both are why settings moved to
`c:\Wise40\settings.json`.
