---
name: stop-tracking-after-tests
description: "Arie wants the telescope's tracking switched off when a test or measurement session ends, not left running"
metadata:
  type: feedback
---

**Turn tracking off when the testing stops.** Asked for on 2026-09-18, after a session of HA slew measurements where it was left on more than once.

**Why:** with tracking on the mount keeps following the sky unattended, so the hour angle climbs toward the `western_haLimit` of +6.5 h and the altitude falls toward the 16° soft limit — the `SafetyMonitorTimer` will eventually `AbortSlew`/`Backoff` on its own, which is not how a session should end. It also leaves the track motor energised on an old analog drive for no reason. Parked with tracking off, the axis simply sits still and the *encoder* hour angle stays put.

**How to apply:**

- After the last slew of a test run, and at the end of any session: `PUT /tracking` with `Tracking=False`.
- Confirm it actually stopped rather than assuming: `Tracking: false`, `Slewing: false`, and `SlewPin` / `PrimaryPins` / `SecondaryPins` all false. `PrimaryIsMoving` can stay true for several seconds afterwards — that is the real settling tail on this axis, see [[slew-time-where-it-goes]], not a failure to stop.
- `AbortSlew` already drops tracking as a side effect, so after an abort there is nothing extra to do.

Do not read this as "park the telescope" — it is only about tracking. Parking is a separate operation and has not been asked for.
