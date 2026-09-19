---
name: ha-angle-distance-is-broken
description: "Angle.ShortestDistance mis-computes distance and direction for AngleType.HA, which ran the primary axis 80 degrees the wrong way into a limit switch"
metadata:
  type: project
---

**An HA-typed slew drives the wrong way and does not stop.** Found 2026-09-19 by doing it to the
telescope. **Not fixed** — the affected callers were reverted or disabled instead.

## The measurement

Park, targeting HA 0 from HA −01h21m44.8s. True distance **1.363 h = 20.4° = 0.357 rad**. The
slewer logged:

```
[HA to 00h00m00.0s from -01h21m44.8s] axisPrimarySlewer at rateSlew:
    remaining (Angle.rad: 5.3502389458)      <- 306 degrees, i.e. 20.4 HOURS
```

then drove **East**, away from the target, the reported distance **growing** every sample:

```
5.3502  5.3504  5.3518  5.3554  5.3613 ...
```

`ChangedDirection` never fired, because the direction was **wrong from the first sample rather
than changing** — the reversal detector compares `startingDistance.direction` with
`currentDistance.direction`, and both were computed by the same broken arithmetic. So nothing
stopped it. It ran **77 s and about 80° of hour angle**, and was stopped by a physical limit
switch — see [[soft-limits-are-not-conservative]].

**Declination was correct in the same slew**: 0.349 rad for a 20° move, decreasing properly. So
this is specific to the HA angle path, not to the slewer.

## Where it is not

`Angle.ShortestDistance` takes the non-periodic branch for `AngleType.HA`, because `Angle`
declares that type `_periodic = false` with bounds −12..+12:

```csharp
result.angle = Angle.FromRadians(Math.Abs(this.Radians - other.Radians), this._type);
result.direction = (this.Radians > other.Radians) ? Decreasing : Increasing;
```

That arithmetic is correct as written. From −1.3624 h to 0 it should give 0.357 rad and
`Increasing`. It gave 5.35 rad and drove the other way, so **the fault is more likely in how a
NEGATIVE hour angle is represented in `Hours`/`Radians` than in the subtraction** — but that was
inferred, not proven. Prove it with a test against known values before changing anything.

Note 5.3502 rad is 20.44 h, not the 22.64 h you would get from a naive 24-hour wrap, so whatever
happens is not simply "treated as 0..24".

## What was done instead

- `Park` and `ParkFromGui` reverted to right-ascension targets (PR #40), undoing PR #33. Park
  sets `Tracking = true` before the slew again, since an RA target is what tracking holds.
- `SlewToHaDecAsync` **throws instead of slewing**.

The second matters more than it looks: `Dash.cs:1170` calls it from the HA/Dec slew button. That
button was harmlessly broken for years by a case-sensitivity bug in the Action parameter parsing;
**fixing that parsing is what made the button work, and therefore what made this reachable.** A
button that returns an error is fine. A button that runs an axis into a limit is not.

## What reverting costs

Exactly the defect PR #33 set out to fix. `Park` samples `LocalSiderealTime` once, before the
slew, which pins the target to the meridian as it was at that instant, so the mount lands about
**15′ west** of it for a 60 s slew. Fifteen arcmin of parking error is much better than an axis
at a limit. See [[park-position]].

## Before re-enabling any of it

1. A test of `Angle.ShortestDistance` over HA-typed angles, **both signs**, against hand-computed
   values. This is pure arithmetic and needs no telescope.
2. Only then reconsider `Park` on an HA target, which remains the right design — an hour angle
   needs no lead for the slew duration.

**Park is what unattended systems call** — ACP parks at the end of a session, and the Dash has a
park button. That is why this could not be left in place while the arithmetic was investigated.
