---
name: ascom-astrometry-one-instance-per-process
description: Disposing — or merely garbage-collecting — an ASCOM NOVAS31 or AstroUtils tears down native state for the whole process and kills it instantly
metadata:
  type: project
---

`ASCOM.Astrometry`'s `NOVAS31` and `AstroUtils` are thin managed façades over a **single native
library holding process-wide state**. Releasing any one instance releases it for everybody.
Measured 2026-09-24, one thread looping `MoonIllumination` while another created and dropped
instances — each killed the process inside a second:

| what the other thread did | how the process died |
|---|---|
| `NOVAS31.Dispose()` | `0xC0000409` — native fast-fail |
| `AstroUtils.Dispose()` | `0xC0000005` — access violation |
| neither: construct, drop, `GC.Collect()` | `ExecutionEngineException` (`0x80131506`) |

The third row is the important one. **Not calling `Dispose` is not enough** — a finalizer does
the same damage. There must be exactly one instance per process and it must never become
garbage.

**Why it mattered here:** `ASCOM.RemoteServer` hosts every Wise40 driver at once, and per-client
teardown reaches shared singletons (see [[wise40-driver-connected-dispose]]). `WiseSite.Dispose`
used to dispose its `novas31`, `ascomutils` and `astroutils`, and the Dome, DavisVantage and
ClarityII drivers each constructed an `AstroUtils` purely to dispose it in their own `Dispose()`.
Any one of those running took the telescope's astrometry down with it.

**How to apply:** `SafeNovas31`, `SafeAstroutils` and `SafeAscomutil` now each hold **one static
instance**, created once, and their `Dispose` deliberately does not touch it — so a wrapper is
cheap to make and safe to drop. Keep it that way. Never call `Dispose` on an ASCOM astrometry
object, never let one be collected, and never `new` one up in a short-lived scope.

Watch for the same shape elsewhere: `Dome/Azimuth.cs` declares `class Azimuth : AstroUtils`, so
every `Azimuth` **is** one and its finalizer runs the lethal teardown. Nothing constructs an
`Azimuth` today, which is the only reason it has not bitten — it is a landmine, not a bug.

This is a real defect and it is fixed, but note it was **not** what was killing the moon action:
see [[ascom-moonillumination-kills-the-process]] for that, and for the hypotheses already
eliminated.
