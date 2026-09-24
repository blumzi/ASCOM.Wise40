---
name: ascom-moonillumination-kills-the-process
description: AstroUtils.MoonIllumination kills ASCOM.RemoteServer with a native fast-fail; keep it out of driver code, and know which four explanations are already ruled out
metadata:
  type: project
---

`ASCOM.Astrometry.AstroUtils.MoonIllumination(jd)` **kills ASCOM.RemoteServer**. Measured
2026-09-24: twenty of twenty-five `moon-position` action calls, six process deaths, every one
with the identical signature:

```
Faulting application: ASCOM.RemoteServer.exe
Exception code: 0xc0000409      (native fast-fail, CRT guard check)
Faulting module:  ucrtbase.dll
Fault offset:     0x0009d132    (the same every single time)
```

No managed exception, no stack, nothing in any log. Once it returned **`NaN`** instead of dying,
which is the same corruption wearing a quieter hat.

Localised by tracing every step of the action in `DebugMoon` and reading the last line written:
it is always between "about to call MoonIllumination" and its return, on the request thread, in
that process. The rest of the process was logging normally 140 ms earlier.

**How to apply:** don't call it from driver code hosted in ASCOM.RemoteServer.
`Telescope/WiseTele.cs`'s `moon-position` action returns `raDeg,decDeg` and nothing else for
this reason. NOVAS `Place` — everything `Moon.Position` uses — has never failed once, in the
chain or in isolation, so the position is safe to serve. The Dash has read
`Moon.Instance.Illumination` on its refresh timer for years without dying; whatever the trigger
is, it is specific to the driver host.

The consumer was ACP's pointing-model script, which now gets the Moon from JPL Horizons with a
local formula as fallback. It lives in the **blumzi/ACP** repository at
`Scripts/Wise/WiseTrainCorrector.vbs` — not in this one, because that tree is ACP's live script
directory. See `.claude/memory/acp-scripts-are-the-live-tree.md` there.

**Do not re-derive these.** Each was tested properly and is NOT the cause:

| hypothesis | how it was ruled out |
|---|---|
| The NOVAS computation itself | 40 consecutive `ComputePosition` calls, then 30 rounds of Position + Illumination + Phase + Distance, out of process: clean |
| A race with the unguarded `Util` in `Angle` | 39 000 guarded `MoonIllumination` calls against 1.6 M raw `ASCOM.Utilities.Util` calls, two threads: clean |
| Thread stack size (0xc0000409 *is* a guard-check failure) | survives 200 calls on a **64 KB** stack |
| Cross-process contention with the Dash | killing the Dash first changed nothing |

Two things *were* found and fixed on the way, both real but neither the trigger: the split
astrometry mutex (see `Common/Const.cs` — `NOVAS31` and `AstroUtils` are one native library and
now share one lock) and the disposal hazard in [[ascom-astrometry-one-instance-per-process]].

The remaining untried remedy, if this ever has to be solved rather than avoided: give the native
library **one dedicated long-lived thread** and marshal every astrometry call onto it, instead of
serialising many threads with a mutex. That removes every concurrency question at once, including
the ones nobody has thought of yet.

Related: [[abandoned-mutex-grants-ownership]], [[wise40-acp-driver-integration]].
