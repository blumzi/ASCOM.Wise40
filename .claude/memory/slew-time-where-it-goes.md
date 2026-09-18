---
name: slew-time-where-it-goes
description: "Where Wise40 slew time goes on both axes, verified on sky 2026-09-18. HA coast is direction dependent; every RA coast figure before that date was measured with a broken stop detector, and the detector is still blind at guide rate"
metadata:
  type: project
---

Measured 2026-09-16 across 52 clean slews (Dec), and 2026-09-18 on HA. Raw material for the first is in `C:\Wise40\Logs\2026-09-16\slews\` — already split per-slew by the `extract-activities` script, with `rates/<slew>/{ra,dec}-{slew,set,guide}` distance-to-target time series. `DebugAxes` captures every relay transition, distance at ~10 ms, and the coast after each motor-off. **The logging is sufficient; nothing more needs instrumenting.**

## The headline (2026-09-16, Dec-dominated)

| | SLEW | SET | GUIDE |
|---|---|---|---|
| motor-on | 595.8 s | **4860.8 s** | 1811.6 s |
| share | 8.2 % | **66.9 %** | 24.9 % |

Mean wall clock **120.6 s/slew**. The slew rate does **over 97 % of the angular distance in 8 % of the time**. Dec is the critical path in 54 of 57 slews.

## Root cause: the coast model was guesswork

There are **no ramp times** in the driver. The only deceleration allowance is one hard-coded `stopMovement` per axis per rate (`WiseTele.cs`, `realMovementParameters`), and nothing ever measured it against the mount.

| handoff | modelled | measured coast | |
|---|---|---|---|
| Dec slew→set | 4.5° | 2°26′–3°16′ | conservative by ~1.6° |
| **Dec set→guide** | **3.00″** | **11.9–38.0″, mean 22.7″** | **wrong by 7.6×** |
| RA slew→set | 3.0° | see below — **direction dependent** | |
| RA set→guide | 30.0″ | **34.0″ mean** (n=3, 2026-09-18) | under-set, overshoots every time |

Two effects on Dec:

1. **Dec cannot stop where it is told.** All **52 of 52** Dec guide phases ran *backwards*, undoing a mean 22.7″ overshoot at 0.79″/s.
2. **The dead zone.** Dec cut the slew motor 4.5° out, coasted ~2.85°, was left 1.2–2.1° short — below the 5.0° needed to re-engage slew — and crawled at 49.6″/s for 87–152 s.

## EVERY PRE-2026-09-18 RA COAST FIGURE IS WRONG

Not imprecise — **measured with a broken instrument**. `raEpsilon` was `2e-3` hours = **108″ per sample** (~1740″/s), so `IsMoving` went false while the axis was still coasting hard. Those numbers are detector-trip distances, not coasts.

| rate | recorded 2026-09-16 (old ε) | true (new ε) | understated by |
|---|---|---|---|
| RA set | 0.9″ mean, n=32 | 34.5″ | **38×** |
| RA slew | 2.08° mean | 2.824° | ~35 % |

So the old conclusions "RA set→guide 30″ — no measurable coast, correct" and "1.846–2.895°, 0 of 8 overshot, so 3.0° is already right" were **artifacts of that bug**. Replaced by `primaryEpsilon = 0.40/(3600·15)` h = exactly **0.400″/sample**. If you find an RA coast number dated before 2026-09-18, distrust it.

## The HA axis, measured 2026-09-18

Previously flagged as unverified. Now done — a west leg, two east legs, and a dither run.

**The slew-rate coast is direction dependent, and that is the dominant cost.** Both legs entered within 2 % of the same speed:

| direction | HA window of the coast | coast |
|---|---|---|
| west | 2.84 → 3.03 h | **2.824°** |
| west | ~3.85 h | 2.699° |
| west | −0.06 → 0.09 h | 2.913° |
| **east** | **3.04 → 2.90 h** | **1.997°** |
| east | 1.61 → 1.47 h | 2.002° |
| east | −1.35 → −1.42 h | 2.017° |

**Means: east 2.005° (n=3, 1 % spread), west 2.812° (n=3).** Eastward is the steadier by a wide margin, and shows no hour-angle trend across 4.4 h of HA.

The second east leg was run *deliberately in the west leg's hour-angle window* to separate direction dependence from a polar-axis imbalance, whose torque would vary as sin(HA). A **41 % gap survived at identical HA** — so it is direction, and the imbalance hypothesis is dead. Eastward is also far steadier: 2.002° and 1.997° across 1.4 h of hour angle.

One shared 3.0° therefore cannot serve both. Eastward it cut 2.951° out, coasted 1.997°, and left 0.955° to crawl at 52″/s — **65.4 s of a 104.5 s slew**. Fixed by `stopMovementIncreasing` (east 2.10°); see PR #29.

**Phase breakdown**, west 34.6° / east 19.6° of travel:

| phase | west | east |
|---|---|---|
| slew motor on | 18.81 s | 10.90 s |
| slew stop-detect | 11.10 s | 5.13 s |
| set motor on | 5.46 s | **62.26 s** |
| set stop-detect | 2.01 s | 2.57 s |
| guide (reversing) | 4.92 s | 11.77 s |
| **total** | **42.34 s** | **92.77 s** |

### Verified on sky 2026-09-18, after the fix

Matched 1.5 h legs east and west, tracking on, run back to back:

| phase | EAST before | **EAST after** | WEST after (control) |
|---|---|---|---|
| slew motor on | 10.90 s | 11.28 s | 10.83 s |
| slew stop-detect | 5.13 s | 7.32 s | 9.72 s |
| **set motor on** | **62.26 s** | **4.19 s** | 3.80 s |
| set stop-detect | 2.57 s | 2.18 s | 2.71 s |
| **guide** | **11.77 s** | **none — skipped** | **none — skipped** |
| **total** | **92.77 s** | **25.00 s** | 27.07 s |

**Eastward is 73 % faster.** The mechanism did exactly what it was built to do: east cut at 2.083° (the new threshold, not 2.951°), coasted 2.017°, and was left **237″** rather than 0.955°. West cut at 2.975° and coasted 2.913°, confirming `stopMovementIncreasing` applies only to the Increasing/East direction and leaves the base 3.0° untouched.

Both directions now skip the guide leg entirely — the set→guide change to 34.5″ lands the axis 1.5–3″ out, inside the 4″ arrival tolerance, so the log reads `too short for rateGuide`. **RA set coast is now n=5: 34.5, 36.0, 31.5, 33.0, 34.5″, mean 33.9″.**

No `ChangedDirection` or `reversal did not hold` fired during either leg.

**Real rates, not the nominal ones.** Slew is **1.80–1.84°/s**, not the 2.0°/s in `Const.cs`. Set ≈ 52″/s, guide 0.6–0.8″/s. The `AxisMonitor` sample period is **59–62 ms**, not the nominal 50, so one `_raDeltas` window (10 samples) spans ~0.6 s.

**Stop detection on HA is mostly real, unlike Dec.** Of the west leg's 11.10 s: **3.6 s** is bulk deceleration (429″→~1″/sample, >97 % of velocity), then a **tail with τ ≈ 2.5 s** while the axis oscillates about the tracking rate at ±6–20″/s as elastic wind-up releases into the track drive. Dec's split was 4.6 s real / 21.1 s detector — HA's is the other way round.

**`primaryEpsilon` is validated.** A 60 s dither run with tracking on: median |ΔRA| **0.043″**/sample, mean 0.048″, max 3.79″, 1 of 965 samples over the 0.400″ threshold. Median is below the 0.1187″ encoder quantum, so the measurement is quantisation-limited. ~9× margin.

**But the detector is BLIND AT GUIDE RATE, and the logs look like data.** Guide runs 0.6–0.8″/s, which over a ~60 ms sample is 0.04–0.05″ — an order of magnitude under the 0.400″ epsilon. Checked against a guide leg that demonstrably ran 20.11 s and covered ~16.5″:

| sample | window max | epsilon | `IsMoving` |
|---|---|---|---|
| 07:44:36.587 | 0.363″ | 0.400″ | **False** |
| 07:44:37.157 | 0.151″ | 0.400″ | **False** |
| 07:44:37.869 | 0.120″ | 0.400″ | **False** |

False throughout, with the motor on. So **every `stopping distance: 00h00m00.0s` at `rateGuide` in these logs is an artifact** — it declares "stopped" instantly because it never saw motion. The guide-rate coast is **unmeasured, not zero**. Same class of error as the old `raEpsilon`, in the opposite direction.

This matters the moment anyone tries to tighten the arrival tolerance. Note that tolerance is **4″, not 3″**: `EnoughDistanceToMove` tests `minimalMovement + stopMovement` = 1″ + 3″. Setting it below the true guide coast is exactly what produced Dec's 52-of-52 reversals. Measure the coast first — from `SampleAxisMovement` positions, since `IsMoving` cannot see it — and fix the epsilon before trusting any guide-rate figure. The proper fix is a **rate-aware epsilon**: 0.400″/sample is right for slew and set (set runs 3.1″/sample, 7.8× above it) and ~10× too coarse for guide. `PrimaryAxisMonitor.IsMoving` currently uses `primaryEpsilon` for both of its branches.

Asked and declined 2026-09-18 (Arie): leave the tolerance at 4″ for now.

**What `raDelta` actually measures.** `raDelta = |Δ(LST − HA)|`, so under tracking the detector is watching **tracking error**, not axis motion — two quantities each ~0.93″/sample that must cancel to under 0.400″. On a bit-for-bit motionless axis, 683 samples gave `raDelta` = 0.93″ ± 0.05″ (pure LST) while `haDelta` was **exactly 0**. The code switches queues on `Tracking` and so is correct in both states, but this is why HA settles slowly and Dec does not. LST itself is smooth — sampling jitter sits ~8× below epsilon and contributes nothing.

## A single bad encoder sample could end a slew leg

Found 2026-09-18 and the biggest single win of the lot. `CurrentPosition()` reads the encoder directly and **bypasses `AxisMonitor.Acceptable()`** — that filter guards only the monitor's own sample queue. The `ChangedDirection` test then ended the leg on **one** reading:

```
07:12:59.375  at rateSlew: at 18h18m27.4s, ChangedDirection ==> target: 05h02m38.2s
07:12:59.460  at rateSlew: at 05h40m55.9s waiting for axisPrimary to stop moving ...
```

The axis was really at 05h40m55.9s. That one spurious sample cut the slew **9.57° short** and handed 6.875° to the set leg — a four-minute stall on a routine 41.6° move. **86 `ChangedDirection` events across the 52 slews of 2026-09-16, ~1.65 per slew.**

This is a strong candidate for the "duration barely depends on distance" puzzle below: random early termination of the fast leg produces exactly that signature. Fixed in PR #26 by requiring the reversal to persist 150 ms — a *duration*, not an iteration count, because `CurrentPosition` is a cached field refreshed every ~59 ms while the loop polls every 10 ms, so one bad reading reaches ~6 consecutive iterations. It caught a real glitch on its first slew.

Related trap: `_maxDeltaRadiansAtSlewRate` is 0.0021 rad against a measured slew-rate step of 0.00208 rad — **1 % margin**, so `Acceptable()` would reject about half of all legitimate slew-rate samples. Very likely why `Predicted()` was neutered to `return reading; // pred;`. Raise that threshold to ~2× the real step before reviving that path. See [[renishaw-encoder-calibration]].

## The counter-intuitive bit

Duration barely depends on distance, and a **3° slew took longer than a 100° slew**:

| RA dist | Dec dist | wall |
|---|---|---|
| 100.75° | 15.07° | 196.4 s |
| 37.80° | 31.72° | 168.3 s |
| **2.99°** | 10.08° | **252.6 s** |

2.994° falls just under the 3.000° RA slew threshold, so slew never engages and the whole 3° goes at 52″/s. **But see the `ChangedDirection` section** — at ~1.65 truncations per slew, some of this scatter is not the threshold at all.

## The fix, and its limit

`stopMovement` should target the **mean** measured coast, not a safe upper bound — the cleanup rate is identical in both directions, so undershooting by X costs exactly what overshooting by X costs. Plan in `.claude/plans/shorten-slew-duration.md`.

**Pulsing the slew motor to synthesise an intermediate rate is off the table** (Arie, 2026-09-17 — too hard on an old analog drive). So moves between ~0.5° and the slew engage threshold stay slow. The 3° cliff moves down but does not disappear.

`TooFarToMoveAtRate` still uses the base `stopMovement` for the engage threshold, deliberately — it decides which distances are worth starting at a rate, a separate question from where to cut the motor. It does mean eastward moves in the ~2–3° band still will not engage slew even though the axis can now stop in 2.10°. Revisit once the directional constant has proven itself.

Expect the **dome** to become the binding constraint: `Slewing` stays true until it finishes (`WiseTele.cs:1099`, `DomeSlaveDriver.cs:103` waits with no timeout) and there is **no dome angular rate anywhere in the code**. Instrumentation is in place (`WiseDome:motion: DONE` lines) but has not yet been read after an ACP session.

## Still unmeasured

- **Both axes at once.** The `ReadyToSlewFlags` rendezvous has never been exercised with both axes doing real travel — every test so far moved one axis. A pointing run drives both. See [[telescope-drive-topology]] for why they cannot simply be decoupled.
- **The guide-rate coast, on either axis.** Blocked on the epsilon above — the detector cannot see guide-rate motion, so it has never actually been measured on any axis.
- **Dec direction dependence at slew rate.** HA turned out to be strongly asymmetric; Dec's `set`-rate coast is 20″ north against 27.9″ south, but its slew-rate coast has never been split by direction.
