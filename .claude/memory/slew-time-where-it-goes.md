---
name: slew-time-where-it-goes
description: "Measured breakdown of Wise40 slew time - 67% is spent at set rate crawling the last degrees because the coast distances were never measured"
metadata:
  type: project
---

Measured 2026-09-16 across 52 clean slews. Raw material is in `C:\Wise40\Logs\2026-09-16\slews\` — already split per-slew by the `extract-activities` script, with `rates/<slew>/{ra,dec}-{slew,set,guide}` distance-to-target time series. `DebugAxes` captures every relay transition, distance at ~10 ms, and the coast after each motor-off. **The logging is sufficient; nothing more needs instrumenting.**

## The headline

| | SLEW | SET | GUIDE |
|---|---|---|---|
| motor-on | 595.8 s | **4860.8 s** | 1811.6 s |
| share | 8.2 % | **66.9 %** | 24.9 % |

Mean wall clock **120.6 s/slew**. The slew rate does **over 97 % of the angular distance in 8 % of the time**. Dec is the critical path in 54 of 57 slews.

## Root cause: the coast model is guesswork

There are **no ramp times** in the driver. The only deceleration allowance is one hard-coded `stopMovement` distance per axis per rate (`WiseTele.cs:582-645`), and nothing ever measured it:

| handoff | modelled | measured coast | |
|---|---|---|---|
| RA slew→set | 3.0° | 1.76–2.85° | conservative |
| Dec slew→set | 4.5° | 2°26′–3°16′ | conservative by ~1.6° |
| RA set→guide | 30.0″ | none measurable | correct |
| **Dec set→guide** | **3.00″** | **11.9–38.0″, mean 22.7″** | **wrong by 7.6×** |

Two effects:

1. **Dec cannot stop where it is told.** All **52 of 52** Dec guide phases run *backwards*, undoing a mean 22.7″ overshoot at 0.79″/s.
2. **The dead zone.** Dec cuts the slew motor 4.5° out, coasts ~2.85°, is left 1.2–2.1° short — below the 5.0° needed to re-engage slew — and crawls at 49.6″/s for 87–152 s.

## The counter-intuitive bit

Duration barely depends on distance, and a **3° slew takes longer than a 100° slew**:

| RA dist | Dec dist | wall |
|---|---|---|
| 100.75° | 15.07° | 196.4 s |
| 37.80° | 31.72° | 168.3 s |
| **2.99°** | 10.08° | **252.6 s** |

2.994° falls just under the 3.000° RA slew threshold, so slew never engages and the whole 3° goes at 52″/s.

## The fix, and its limit

`stopMovement` should target the **mean** measured coast, not a safe upper bound — the cleanup rate is identical in both directions, so undershooting by X costs exactly what overshooting by X costs. Today's conservatism buys nothing. Plan in `.claude/plans/shorten-slew-duration.md`.

**Pulsing the slew motor to synthesise an intermediate rate is off the table** (Arie, 2026-09-17 — too hard on an old analog drive). So moves between ~0.5° and the slew engage threshold stay slow: using slew rate there would mean a sub-second pulse. The 3° cliff moves down but does not disappear.

Expect the **dome** to become the binding constraint afterwards: `Slewing` stays true until it finishes (`WiseTele.cs:1099`, `DomeSlaveDriver.cs:103` waits with no timeout) and there is **no dome angular rate anywhere in the code** — it has never been measured.

## ALMOST ALL OF THIS IS DEC. HA IS LARGELY UNVERIFIED

Flagged by Arie, 2026-09-17, and it bounds how much of the day's work is actually confirmed. **Every timing test was a Dec-only move with RA essentially stationary** — 0.5° and 20° in declination, nothing that drove the hour-angle axis any distance.

What HA *is* backed by: slew-rate travel-after-threshold of 1.846–2.895° (n=8, from log forensics, not a controlled test), which is what says its 3.0° `stopMovement` is already right — 0 of 8 legs overshot. Its `set`→`guide` threshold of 30″ looks right too, since RA guide legs exit `CloseEnough` in 3–8 s without reversing.

What HA has **no** measurement behind:

- **`primaryEpsilon`** (0.40″/sample = 8″/s, replacing the unreachable `raEpsilon`/`haEpsilon`) was never exercised on the mount. The value was derived from the HA encoder's 0.1187″ quantum by analogy with Dec, **not** from observed dither — nobody has watched a stationary HA axis the way we watched Dec sit on four adjacent counts for 21 seconds.
- **HA stop-detection time.** The 4.6 s real / 21.1 s detector split is a Dec measurement. HA's may differ: different drum, different inertia, and a coarser encoder.
- **Direction dependence.** Dec's `set`-rate coast is 20″ north against 27.9″ south. East/west asymmetry on HA is plausible — the axis is loaded differently either side of the meridian — and completely unmeasured.
- **RA's "no measurable coast" at `set` rate** may just mean "below the 30″ threshold", not zero.
- **Both axes working at once.** The `ReadyToSlewFlags` rendezvous has never been exercised with both axes doing real travel; a Dec-only move leaves RA skipping straight to guide. A pointing run does drive both.

To do it properly: a large RA-only move in each direction, with the same log forensics — travel after the threshold trips, coast at each rate, and stop-detection time separated into real deceleration versus detector lag. Then a diagonal move to exercise the rendezvous.

See [[telescope-drive-topology]] for why the axes cannot simply be decoupled.
