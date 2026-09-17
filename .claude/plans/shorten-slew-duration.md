# Shorten Wise40 slew duration

## Context

Slews take a mean of **120.6 s** and the duration barely depends on distance — a 2.99° slew
took 252.6 s while a 100.75° slew took 196.4 s. This sets the ceiling on every pointing run:
at ~3.6 min/point, a 50-point pointing model costs about 3 hours of telescope time.

Measured across 52 clean slews on 2026-09-16 (`C:\Wise40\Logs\2026-09-16\slews\`, already
split per-slew, with `rates/<slew>/{ra,dec}-{slew,set,guide}` time series):

| | SLEW | SET | GUIDE |
|---|---|---|---|
| motor-on | 595.8 s | **4860.8 s** | 1811.6 s |
| share | 8.2 % | **66.9 %** | 24.9 % |

The slew rate does **over 97 % of the angular distance in 8 % of the time**. Everything else
is the last couple of degrees being crawled.

Root cause is not ramp times — **the driver has none**. The only deceleration allowance is a
single hard-coded `stopMovement` distance per axis per rate, and nothing ever measured it
against the mount:

| handoff | modelled | measured coast | verdict |
|---|---|---|---|
| RA slew→set | 3.0° | 1.76–2.85° (mean ~2.3°) | conservative |
| Dec slew→set | 4.5° | 2°26′–3°16′ (mean ~2.85°) | conservative by ~1.6° |
| RA set→guide | 30.0″ | none measurable | correct |
| **Dec set→guide** | **3.00″** | **11.9–38.0″, mean 22.7″** | **wrong by 7.6×** |

Two consequences:

1. **Dec physically cannot stop where it is told.** All **52 of 52** Dec guide phases run
   *backwards*, undoing a mean 22.7″ overshoot at 0.79″/s.
2. **The dead zone.** Dec cuts the slew motor 4.5° out, coasts ~2.85°, and is left 1.2–2.1°
   short — below the 5.0° needed to re-engage slew, so it crawls at 49.6″/s for 87–152 s.

Decisions taken: **pulsing the slew motor is off the table**, so the three fixed rates stay as
they are. Arrival tolerance goes to **~3″ on both axes**, keeping a guide leg but making it
converge forwards instead of reversing — and these numbers are to be reviewable without a
rebuild.

## The key insight

`stopMovement` should be aimed at the **mean** measured coast, not a safe upper bound. The
cleanup rate is the same in both directions, so undershooting by X costs exactly what
overshooting by X costs. Today's conservatism buys nothing and costs the whole SET leg.

## Changes

### 1. Retarget the coast constants — `Telescope/WiseTele.cs:582-645`

`realMovementParameters`, `stopMovement` only. Leave `minimalMovement` alone.

| axis / rate | from | to | note |
|---|---|---|---|
| RA slew | `"00h12m00.0s"` (3.0°) | `"00h09m12.0s"` (2.3°) | mean measured coast |
| RA set | `"00h00m02.0s"` (30″) | unchanged | already correct |
| RA guide | `"00h00m00.1s"` (1.5″) | `"00h00m00.2s"` (3″) | arrival tolerance |
| Dec slew | `"04:30:00.0"` (4.5°) | `"02:51:00.0"` (2.85°) | mean measured coast |
| **Dec set** | `"00:00:03.0"` | `"00:00:23.0"` | **match the 22.7″ coast** |
| Dec guide | `"00:00:00.1"` | `"00:00:03.0"` | arrival tolerance |

Mind the parsers: `Angle("00h…")` is **HMS** (×15 for arc) on the primary axis,
`Angle("00:…")` is **DMS** on the secondary — see `Common/Angle.cs:109-158`.

Expected on Dec, the critical path in 54 of 57 slews: set leg 78.6 s → ~16 s, guide leg
31.4 s → ~4 s, Dec wall ~132 s → **~45 s**.

### 2. Make them profile-backed, with these values as the code defaults

Add read/write in `WiseTele.ReadProfile`/`WriteProfile` (`WiseTele.cs:~3958`, `~3980`) and
names in `Common/Const.cs` alongside `Telescope_EncodersInUse`, keyed per axis per rate.

**The default is what matters, not the persisted value.** An elevated rebuild re-registers the
driver for COM and **wipes the profile subkey** — that is how the deliberate switch to
`EncodersInUse = New` was silently lost earlier today. The profile is for tuning between
rebuilds; the code default must always be the value the telescope should actually run on.

### 3. Fix the debug throttle and the work done to build debug strings

- `WiseTele.cs:2321-2331` — `byte count = 0;` is declared **inside** the loop body, so
  `(count %= 5) == 0` is always true and `count++` is dead. A formatted line is emitted every
  10 ms per axis. Hoist the counter out of the loop.
- `AxisMonitor.cs:291-297` and `:507-512` — the debug strings evaluate
  `renishawHaEncoder.Position` **and** `.Radians`, i.e. two extra BiSS reads per axis per
  50 ms sample, unconditionally. Guard with `Debugger.Debugging(level)` before building.
- `Common/Debugger.cs:137-180` — `WriteLine` takes an interpolated string, so the cost is paid
  at the call site before the level check; it also calls `Process.GetCurrentProcess()` per line
  and runs with `AutoFlush = true`.

This is not cosmetic: the night produced a **2.03 GiB** log, and synchronous flushes plus extra
encoder reads perturb the very timing being tuned.

### 4. Take the not-decreasing check out of the `else if` chain — `WiseTele.cs:2223-2235`

`prevDistance` is declared `0.0` per rate at `:2177` and assigned at `:2339`, *outside* the
`while(true)` that closes at `:2338` — so it is always 0.0 inside the loop and the branch never
fires. Harmless today, but it sits in an `else if` chain **above** the `CloseEnough` test at
`:2236`. Anyone repairing the dead assignment by moving it inside the loop would disable the
arrival check on every leg, leaving overshoot and timeout as the only exits. Make it an
independent `if` that only logs, or delete it.

### 5. Loosen the per-leg caps — `WiseTele.cs:593, 602, 611, 623, 632, 641`

Dec set currently needs 270 s against `maxTime = 5 min`. After change 1 it needs ~30 s, so the
margin stops mattering — but raise the caps to 10 minutes anyway so a bad coast degrades into a
slow slew rather than an `AbortSlew`. Two slews hit the cap on 2026-09-16
(`C:\Wise40\Logs\2026-09-16\suspects.txt`).

### 6. Optional, later — rendezvous only at `rateSlew`

`ReadyToSlewFlags` (`WiseTele.cs:2125-2150`) holds both axes in lockstep at *every* rate. That
is a hardware necessity only at slew rate, because `SlewPin` is a single relay shared by all
four direction motors (`WiseTele.cs:515`, `WiseMotor.cs:111-123`). `set` and `guide` use
per-motor pins and need no rendezvous. RA sits idle 56 % of every slew. This mostly frees RA
rather than shortening wall clock, since Dec is the critical path — do it after 1–5 land.

## Staging

**Stage 1 — Dec set→guide 3″→23″, plus both arrival tolerances.** Kills the 52/52 reversals
and leaves slew-rate behaviour untouched. No new overshoot risk.

**Stage 2 — the slew→set retarget.** The larger win, and the one that introduces deliberate
slew-rate overshoot on roughly half of slews (up to ~0.45°, recovered at set rate in ~30 s).
Verify Stage 1 on sky before starting Stage 2.

## Verification

The existing `DebugAxes` logging plus the per-slew extraction already answer this; no new
instrumentation is needed. After each stage, on a night's slews:

1. Recompute the SLEW/SET/GUIDE split from `Motor.SetOn at rateX` / `SetOff` pairs.
2. **Count Dec guide direction reversals.** Baseline 52/52. Stage 1 target: near zero.
3. Mean wall clock from the `total-duration:` lines. Baseline 120.6 s. Target under 60 s.
4. Dec set leg duration. Baseline 78.6 s. Target under 30 s.
5. Check `suspects.txt` is empty — no leg hitting `maxTime`.
6. Confirm pointing still lands within tolerance: the `renishaw-calibration.csv` residuals and
   ACP's reported pointing error should be unchanged, since none of this alters where the
   telescope goes, only how fast it gets there.

Build elevated (COM registration needs it) and restart the chain; then re-check that
`EncodersInUse` is still `New` and the new parameters read back as intended.

## Two limitations to expect

- **Moves between ~0.5° and the slew engage threshold (~2.8°) still run entirely at set
  rate** — 36 to 200 s. Using slew rate there means a sub-second pulse, which is off the table.
  The 3° cliff moves down but does not disappear.
- **The dome may become the binding constraint.** `Slewing` stays true until the dome finishes
  (`WiseTele.cs:1099`, `DomeSlaveDriver.cs:103` waits with no timeout), and there is no dome
  angular rate anywhere in the code — it has never been measured. If axis time drops to ~45 s,
  measure the dome before expecting the pointing run to get faster.
