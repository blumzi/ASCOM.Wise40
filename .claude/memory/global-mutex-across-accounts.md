---
name: global-mutex-across-accounts
description: "The Wise40 chain spans two accounts - Wise40Watcher is LocalSystem, the Dash and ASCOM hubs are the logged-in user - so any Global\\ kernel object needs an explicit DACL, and creating one in a static initializer kills every driver in the process"
metadata:
  type: reference
---

**A `Global\` named kernel object created without an explicit DACL is reachable only by the
account that created it.** The Wise40 chain spans two accounts, so this bites every time:

- `Wise40Watcher` runs as **LocalSystem** (`sc qc Wise40Watcher` -> `SERVICE_START_NAME:
  LocalSystem`) and launches the drivers.
- The Dash, `ASCOM.OCH.Server.exe` and the other ASCOM hubs run as the **logged-in user**.

Whichever side starts second gets `UnauthorizedAccessException: Access to the path
'Global\<name>' is denied`.

## Why it cost a day

`WiseProfile` created its cross-process lock in a **static field initializer**:

```csharp
private static readonly Mutex fileMutex = new Mutex(false, @"Global\Wise40Settings");
```

A throw there becomes a `TypeInitializationException`, which poisons the type for the life of
the process and cascades through everything that touches it:

```
TargetInvocationException
 +- TypeInitializationException: 'ASCOM.Wise40.TessW.ObservingConditions'
     +- TypeInitializationException: 'ASCOM.Wise40.Common.Exceptor'
         +- TypeInitializationException: 'ASCOM.Wise40.WiseSite'
             +- TypeInitializationException: 'ASCOM.Wise40.Common.WiseProfile'
                 +- UnauthorizedAccessException: Access to the path 'Global\Wise40Settings' is denied
```

Both ObservingConditions drivers died this way on 2026-09-21. Because `Exceptor` sits near the
root of `Common`, a settings-layer fault took down drivers that have nothing to do with settings.

## The trap that wasted the diagnosis

**It only reproduces across accounts.** Loading the assembly and constructing the type in a test
process passes, because that process creates the object as its own user and therefore owns it.
An in-process repro of this class of bug proves nothing - the hypothesis was raised, "tested",
wrongly marked disproved, and only the OCH trace log settled it.

Corollary: the ASCOM trace logs under `Documents\ASCOM\Logs <date>\` carry the **full inner
exception chain** when a driver fails to connect; the dialog shows only the outermost
`TargetInvocationException`. Go to `ASCOM.OCH.*.txt` first. Mind the folder date - ASCOM names
the folder for when the log was *created*, so a long-running process keeps writing into an
earlier day's folder.

## The fix

`Common/WiseProfile.cs`, `CreateFileMutex()` - grant Everyone at creation so either account can
open it, and never let the method throw:

```csharp
MutexSecurity security = new MutexSecurity();
security.AddAccessRule(new MutexAccessRule(
    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
    MutexRights.Synchronize | MutexRights.Modify | MutexRights.ReadPermissions,
    AccessControlType.Allow));
bool createdNew;
return new Mutex(false, @"Global\Wise40Settings", out createdNew, security);
```

with a `catch` falling back to `Local\Wise40Settings`, then to an unnamed mutex. The DACL applies
only at **creation**, so an object left over from a pre-fix LocalSystem process still denies
access - the fallback is what keeps the driver alive until that process exits.

`WaitOne` also catches `AbandonedMutexException`: it means a previous holder died and we *did*
get the lock, but unhandled it propagates out of whatever callback thread was writing and
terminates the process.

**Rule of thumb: nothing in `Common`'s static initialization path may throw.** See
[[wise40-build-and-environment-gotchas]] and [[common-dll-shadowing]].
