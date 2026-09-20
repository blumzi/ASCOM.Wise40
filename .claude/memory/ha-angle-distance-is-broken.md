---
name: ha-angle-distance-is-broken
description: "RESOLVED 2026-09-20 - two defects made an HA-typed slew run an axis into a limit: FromRadians was 15x wrong for HMS types, and the direction reached the wrong motor. Both fixed and proven on sky"
metadata:
  type: project
---

> **RESOLVED 2026-09-20.** Both defects below are fixed and covered by `TestAngleHa`. The second
> one — the direction — was fixed by `Angle.MechanicalDirection`, which inverts for `AngleType.HA`
> because `movementDict` is keyed in right-ascension sense and hour angle grows the other way.
>
> Proven on sky the same day: two 0.9° legs, west then east, landing **0.02′** from target each
> time, with a wrong-way abort armed at 7′ that never fired. Hour-angle targets are also *more*
> accurate than RA ones for this, since they do not drift with the meridian — compare the 24′ of
> pure lead error in [[park-position]].
>
> `Park` and `ParkFromGui` still use RA targets deliberately; moving them is a separate decision.
>
> Getting there needed [[common-dll-shadowing]] solved first: the fix was compiled and deployed
> but a stale `Common.dll` from another driver's folder was what actually loaded.

**An HA-typed slew drove the wrong way and did not stop.** Found 2026-09-19 by doing it to the
telescope. It turned out to be **two independent defects**:

| | status |
|---|---|
| `Angle.FromRadians` was 15× wrong for HMS types, so the distance was 15× too large | **fixed**, PR #44 |
| `movementDict` maps `axisPrimary` + `Increasing` to `EastMotor` whatever the angle type | **OPEN** |

**HA-typed slews remain disabled, and should.** With the distance now correct, an HA target would
compute the right magnitude and still drive the wrong way. `Park` stays on a right-ascension
target.

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

**`ChangedDirection` never fired, and correctly so** — this was first written up as the detector
being fooled by a wrong direction, which is not what happened. The reversal detector compares
`startingDistance.direction` with `currentDistance.direction`, and both were `Increasing`
throughout, which was *right*: the target stayed west of the axis the whole time, because the axis
was running east. There was no reversal to detect. Driving away from the target is invisible to a
detector that only watches for the direction *flipping*.

So nothing stopped it. It ran **77 s and about 80° of hour angle**, and was stopped by a physical
limit switch — see [[soft-limits-are-not-conservative]].

Worth noting as a gap in its own right: the slewer has no check for "distance to target is
increasing". There is a dead branch for exactly that at `WiseTele.cs` — `prevDistance` is assigned
outside the loop, so it is always 0.0 inside and the test never fires, and its body only logs
`SUSPECT: distance to target is INCREASING` with the `status = Failed` commented out. A working
version of that check would have caught this in a second or two, whatever the cause.

**Declination was correct in the same slew**: 0.349 rad for a 20° move, decreasing properly. So
this is specific to the HA angle path, not to the slewer.

## What it actually was

Settled by `TestAngleHa`, which reproduces the whole thing in under a second with no telescope.
**Two guesses recorded here before that test were wrong**, and both are worth stating because they
are the natural things to suspect:

- **"Negative hour angles are mishandled."** No. They round-trip exactly, `Hours` and `Radians`
  both correct from −12 to +12. That case is in the test *because* it rules this out.
- **"The direction was inverted."** No. `ShortestDistance` returned the correct `AxisDirection` in
  all 14 test cases — zero direction failures. It is not why the axis went the wrong way.

The real fault was a units confusion worth exactly **15**, degrees per hour:

```csharp
public static Angle FromRadians(double rad, AngleType type = AngleType.Deg)
{
    return new Angle(rad * 180.0 / Math.PI, type);   // DEGREES, whatever the type
}
```

For an HMS type the constructor reads that number as **hours**. 0.356675 rad is 20.436 degrees,
stored as 20.436 hours — hence 5.3502 rad reported for a 0.357 rad move.

**Why only hour angles showed it:** `HA` is the only `AngleType` that is both non-periodic *and*
HMS. `Dec` takes the same non-periodic branch of `ShortestDistance` but is degree-based, so
radians→degrees is right. `RA` and `Az` are periodic and take the *other* branch, which was already
guarded by `_isHMS`. The correct pattern existed four times over in the same file.

**RA was silently affected, not unaffected:** `RaFromRadians(1.701696)` should give 6.5 h and gave
**1.5 h**, because RA is periodic over 0..24 and 97.5 mod 24 is 1.5. A plausible wrong answer is
worse than an obvious one. Its only caller is `PrimaryAxisMonitor.Velocity()`, which has no callers
itself, so the damage was a wrong readout rather than wrong control.

Also wrong by the same 15×, and fixed with it: `RaFromRadians`, `HaFromRadians`, `Angle.Min` and
`Angle.Max`.

**The lesson, since two reasoned guesses missed it:** this was pure arithmetic, testable in
minutes without hardware. Reach for the test before the explanation.

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

The arithmetic is done — `TestAngleHa` covers HA `ShortestDistance` and the radians→HMS
conversions, both signs, and passes. What remains is the direction mapping:

```csharp
[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] =
    new MovementWorker(new WiseVirtualMotor[] { EastMotor }),
```

`Increasing` **right ascension** is east. `Increasing` **hour angle** is **west**. The dictionary is
keyed on a direction whose meaning depends on the angle type, and nothing tells it which it has.

Two defensible fixes, and this is a design choice rather than a bug fix:

- **Key it by angle type as well**, so an HA target maps `Increasing` to `WestMotor`. Keeps the
  time-independence that made an HA target attractive.
- **Normalise HA targets to RA before the slewer sees them.** Less invasive and arguably where the
  conversion belongs, but it gives up that time-independence and brings back the slew-duration
  lead — see [[park-position]].

Decide it deliberately, not in the same motion as a bug fix.

**Park is what unattended systems call** — ACP parks at the end of a session, and the Dash has a
park button. That is why this could not be left in place while the arithmetic was investigated.
