---
name: common-dll-shadowing
description: "Rebuilding Common.dll or Hardware.dll is NOT enough - every Wise40 driver carries its own copy and the first one loaded wins for the whole process, so a fresh build can be silently ignored at runtime"
metadata:
  type: reference
---

**A rebuilt `Common.dll` can be compiled, deployed, verified green — and never execute.**

Discovered 2026-09-20 after an hour-angle slew failed with

```
MissingMethodException: Method not found:
  'AxisDirection ASCOM.Wise40.Common.Angle.MechanicalDirection(AxisDirection, AngleType)'
  at ASCOM.Wise40.WiseTele.ScopeAxisSlewer(Angle targetAngle)
```

even though `Common` had just been rebuilt with that method and the deploy had verified the new
telescope binary was answering.

## Why

`ASCOM.RemoteServer` hosts **several Wise40 drivers in one process** — telescope, filter wheel,
focuser, dome, SafeToOperate. Each is COM-registered with a `CodeBase` pointing into **its own**
`bin\x86\Debug`, and each of those folders carries its own copy of `Common.dll` and `Hardware.dll`.

Assembly identity is `Common, Version=1.0.0.0, PublicKeyToken=5a596dde3293c610` for all of them. So
**whichever driver activates first supplies `Common` for the entire process**, and every other
driver binds to that already-loaded assembly. The filter wheel's copy was three days old, so the
freshly built telescope bound to a `Common` predating the method it had been compiled against.

**It is a race, not a constant.** It depends on activation order, which is why some earlier `Common`
and `Hardware` changes appeared to take effect and others did not. Do not conclude from one working
test that a Common change is live.

## How to check which copy is actually loaded

A loaded assembly's file is **locked**. Open it for write: success means it is *not* loaded.

```powershell
try { $fs=[IO.File]::Open($path,'Open','ReadWrite','None'); $fs.Close(); 'free' } catch { 'LOADED' }
```

Two traps in reading that result:

- **Access-denied is not a lock.** Anything under `Program Files` fails to open for write without
  elevation, which looks identical. A 2021 copy there was blamed for an hour on that basis; it was
  innocent.
- `Process.Modules` did **not** list the managed assemblies for these 32-bit hosts, even from a
  32-bit PowerShell. The lock test is what worked.

Reflection tells you whether a given file is current without running anything:

```powershell
$a=[Reflection.Assembly]::Load([IO.File]::ReadAllBytes($p))
($a.GetTypes()|? Name -eq 'Angle').GetMethod('MechanicalDirection')
```

## The fix, and what it does not cover

The elevated deploy now has a SYNC step: after building, it copies the freshly built `Common.dll`
and `Hardware.dll` over **every x86 copy** in the repo and under the ASCOM install, and refuses to
start the chain if any remain stale. First run replaced **19** copies.

**Only x86 copies are synced.** `Common` builds as X86, so writing it into an AnyCPU `bin\Debug`
slot would change that project's architecture. About 20 AnyCPU copies of `Common.dll` and 9 of
`Hardware.dll` are still older; the deploy lists them rather than touching them. Two of those are
loaded by other processes — `Wise40Service\bin\Debug` (the watcher) and `TessW\bin\Debug` (the OCH
server) — so that gap is real, just not on the telescope's path.

## What this casts doubt on

Anything measured *before* this was found, where the change lived in `Common` or `Hardware`, may
have been running old code. Specifically: PR #44's `FromRadians` fix was **not live** in the chain
at the time it was verified — the loaded `Common` had no `IsHms`. The `WisePin` lock split in
Hardware (#38) showed a real improvement on the mount, which suggests the repo copy won the race
that day, but that is inference, not proof.

See [[wise40-build-and-environment-gotchas]] for the rest of the build traps.
