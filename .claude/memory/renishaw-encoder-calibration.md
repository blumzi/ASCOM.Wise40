---
name: renishaw-encoder-calibration
description: "In-progress work to calibrate the Wise40 Renishaw encoders from plate-solved images - the hardware facts, what was fixed, and what the next night needs to produce"
metadata: 
  node_type: memory
  type: project
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-09-17T02:34:42.116Z
---

Live work as of 2026-08-22. The goal is to make the Renishaw encoders trustworthy enough to switch `EncodersInUse` to `New`, calibrated against the sky rather than against the old encoders.

## The hardware, which is not what the code's comments imply

RESOLUTE read heads, but reading an **RLA linear tape**, not a rotary ring. The tape is a **partial arc** on a drum on each axis — longer than the axis travel in both directions, so there is **no seam and no wrap-around**. **One read head per axis.**

Resolution is **1 nm**. From the calibration constants the implied drum diameters are **~222.5 mm (HA)** and **~597.8 mm (Dec)**, which Peter confirmed look about right. Those cannot be measured physically, so the scale has to come from sky data.

Derived numbers worth keeping: 0.119″ of HA and 0.044″ of Dec per logged count (`jitterBits = 6`, so one logged count is 64 raw); 1 second of clock error is 15″ of HA.

## What the calibration must actually determine

**Not the zero-point** — it is exactly degenerate with a pointing model's index error, and ACP will absorb it. **The scale and the linearity**, because a scale error is not absorbable by a pointing model.

Three effects are confusable and only **range** separates them, not point count:

- scale error → residual grows **linearly** with axis angle
- drum eccentricity → **sinusoidal**, once per revolution. With one head nothing cancels it, and 0.05 mm of runout on the HA drum is **93″**
- polar misalignment (suspected, no pointing model built yet) → sinusoidal in HA with a **coupled Dec signature**

A line and a sine are indistinguishable over a short arc, which is why the 2024 two-point fit could not have revealed any of this. An hour of tracking on one field is **not** adequate: it spans only 15° and gives Dec nothing at all.

## How the data is collected

`AcpScripts\Wise40 Calibration Run.vbs` (a copy of ACP's `Test Pointing.vbs`, original preserved alongside) walks an all-sky mesh and calls `Telescope.Action "calibration-point", "<ra>,<dec>"` after each solve. That writes a row to `C:\Wise40\Logs\<date>\renishaw-calibration.csv`.

**Why it is not tied to a sync:** ACP's pointing runs never sync, anywhere.

**Why the script converts to topocentric first:** PinPoint solves in J2000, the driver declares `equTopocentric` in ACP mode and forms `HA = LST - RA`. Handing over J2000 would be wrong by ~21 arcmin of hour angle in every point, consistently — the fit would look excellent and be wrong.

**Settled 2026-08-31: ACP's `Telescope` object does NOT pass `.Action()` through.** The script now creates its own `ASCOM.DriverAccess.Telescope` (ProgID `ASCOM.AlpacaDynamic1.Telescope`) once and reuses it. `AcpScripts\Wise40 Action Test.vbs` is the ten-second check to re-run after any ACP or driver upgrade.

That client must never touch `.Connected` or `.Dispose` — see [[wise40-driver-connected-dispose]]. Both scripts carry the reasoning inline. Both are installed in `C:\Program Files (x86)\ACP Obs Control\Scripts\` (copying needs elevation) and hash-match the repo as of 2026-08-31.

## The finding that inverts the old assumption

`SyncToCoordinates` computed `ha = RightAscension - lst` — the negative of the hour angle — and the 2024 Renishaw HA constants were derived from those values. Both were fixed (`0d5a4e8`). Sign-corrected, the Renishaw reproduces the two sync points that do **not** define the fit to under 0.0007 h, while the **old** encoder was out by a consistent 0.043 h. So the Renishaws may already be good and the old encoders may be what disagrees with the sky — the opposite of what we assumed. Four points from one night is not enough to settle it; the calibration run should.

## RESULT — the run happened 2026-09-16, and the Renishaws won

16 points, HA −4.885h to +4.310h (138°), Dec −9.6° to +57.3°. `C:\Wise40\Logs\2026-09-16\renishaw-calibration.csv`.

**No scale error.** HA residual vs HA slopes 3.76″/h at R²=0.10; Dec residual vs Dec −0.42″/deg at R²=0.07. That was the whole question — scale is the one term a pointing model can't absorb.

**The decisive statistic:** the two encoder systems agree with *each other* to **9.5″ sd in HA, 8.5″ in Dec** — three times tighter than either agrees with the sky (~28–32″). Independent sensors on different drums don't share an error, so the ~30″ residual is the **mount**, not a sensor. Never "correct" an encoder for it.

**Applied:** Renishaw HA zero point **+0.192857117 h** subtracted in `RenishawHaEncoder.Radians` (mean residual 10414.3″ → 0.0″, sd unchanged at 28.1″ — the sd not moving is what proves it was purely a zero point). **Dec deliberately untouched**: +3.06″ against an SEM of 8.12″, i.e. zero. Old encoders for contrast: −31.1′ HA, +3.7′ Dec.

**The real remaining signal:** Dec error growing at **9.5″/h with hour angle**, and the old encoder shows it at 9.2″/h with near-identical R² (0.47 / 0.48). Polar misalignment, definitively the mount. Needs a sinusoid fit and far more HA coverage — belongs in an ACP pointing model, which is now legitimate to build since there's no scale error to bake in.

`EncodersInUse` now defaults to **New** in code, and the digest's `deltaHA`/`deltaDec` are explicitly Renishaw-minus-old so the cross-check survives the switch. Live check: 31.36′ and 3.64′ — the old encoders' own errors, matching ACP's independent sky measurement.

## The old ACP pointing model was cleared, 2026-09-17

`C:\Users\Public\Documents\ACP Config\Active.clb` held 28 observations — **25 from July 2021** and 3 from 2026-09-11 (the sign-bug-era sync night). Its mean recorded error was **RA 33.36′, Dec 3.53′**, i.e. essentially a five-year record of the *old encoders' zero-point error* — the very thing the driver fix removed. Left active it would have pushed the mount ~33′ the wrong way, nearly twice the 17.9′ field, breaking local solves again. RA scatter was 8.78′ sd, so nothing in it was worth keeping.

Deleted (ACP recreates it; absent-file is the fresh-install path) with a backup at `Active.clb.pre-renishaw-2026-09-17`. `CorrectScope` and `AutoMapping` are both `True` in `HKLM\SOFTWARE\WOW6432Node\Denny\ACP\General`, so a new model accumulates from the next run. **Close ACP before touching that file** — it holds the model in memory and can rewrite it on exit.

## Still open

- **`through_pole` was 0 on all 16 rows** (max Dec 57.3°). That path has never run against real data.
- **Solve delay contaminates every point and isn't measurable.** Counts are read when the Action fires — after exposure *and* solve — not at exposure midpoint. Gaps ran 3.4–77.5 min because every solve fell back to all-sky. Log the exposure midpoint before trusting the scatter.
- **Why local solves failed:** pointing error 31′ vs a 17.9′ field. `AcquireSupport.wsc` hardcodes `CatalogExpansion = 0.33` → catalog reaches 11.9′ while the image edge sits 22.1′ away, so the regions never overlapped. **There is no ACP preference for this** and the spiral search is gone (`hstep`/`vstep` declared, never used). Fixing the zero point was the fix.

See [[wise40-build-and-environment-gotchas]] and [[wise40-acp-driver-integration]].
