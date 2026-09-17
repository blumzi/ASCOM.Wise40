# Renishaw encoder calibration against the sky

This folder holds the raw data behind `HaZeroPointCorrectionHours` in
`Hardware/RenishawEncoder.cs`. Without it that constant is an unexplained number.

The originals are written by `Telescope/RenishawCalibrationLog.cs` to
`C:\Wise40\Logs\<date>\renishaw-calibration.csv`, which is a **log** directory and
subject to cleanup — hence the copy here.

## Collecting a run

`AcpScripts\Wise40 Calibration Run.vbs` (a copy of ACP's `Test Pointing.vbs`, original
preserved beside it) walks a sky mesh and calls, after each plate solve:

    Telescope.Action "calibration-point", "<ra hours>,<dec degrees>"

Coordinates must be **local topocentric** — PinPoint solves in J2000, the driver declares
`equTopocentric` in ACP mode and forms `HA = LST - RA`. Handing over J2000 would be wrong
by ~21 arcmin of hour angle in every point, consistently, and the fit would look excellent
while being wrong.

ACP's `Telescope` object does **not** pass `Action()` through, so the script creates its own
`ASCOM.AlpacaDynamic1.Telescope`. See `AcpScripts\Wise40 Action Test.vbs`.

## renishaw-calibration-2026-09-16.csv — 16 points

HA −4.885h to +4.310h (138°), Dec −9.6° to +57.3°.

| measurement | Renishaw | old encoders |
|---|---|---|
| HA zero point | **+0.192857117 h** (+10414.3″, SEM 7.0″) | −31.1′ |
| Dec zero point | +3.06″ (SEM 8.12″ — indistinguishable from zero) | +3.7′ |
| HA residual vs HA | 3.76 ″/h, R² = 0.10 | 6.47 ″/h, R² = 0.25 |
| Dec residual vs Dec | −0.42 ″/deg, R² = 0.07 | −0.69 ″/deg, R² = 0.20 |

**No scale error on either axis.** That was the point of the exercise: a scale error is the
one term a pointing model cannot absorb, while a zero point is exactly degenerate with its
index error.

**The decisive statistic** is that the two encoder systems agree with *each other* far
better than either agrees with the sky:

| | sd | against the sky |
|---|---|---|
| Renishaw − old, HA | **9.5″** | 28.1″ |
| Renishaw − old, Dec | **8.5″** | 32.5″ |

Independent sensors on different drums, different technology, do not share an error. So the
~30″ that remains is the **mount**, not a sensor — do not try to calibrate it away. Most of
it is a Dec error growing at **9.5 ″/h with hour angle**, which the old encoder shows too at
9.2 ″/h with the same R² (0.47 vs 0.48): a polar misalignment signature that belongs in a
pointing model.

Applying the HA correction takes the mean residual from 10414.3″ to 0.0″ with the sd
unchanged at 28.1″. The sd not moving is what proves it was purely a zero point.

Cross-checks that agree independently: the old encoders' errors (31.4′ HA, 3.6′ Dec) match
both the pointing error ACP measured on the sky that night (30.9′/31.3′) and the driver
digest's `deltaHA`/`deltaDec` after the fix (31.36′/3.64′).

## Reading these files — one trap

**Fit on `ha_count` / `dec_count`, not on `renishaw_ha_hours` / `renishaw_dec_deg`.**

The raw counts mean the same thing forever. The derived columns do not: this file predates
the zero-point correction, so its `renishaw_ha_hours` is 0.192857 h high, while every file
written after 2026-09-17 has the correction already applied and should scatter about zero. A
fit spanning both, using the hours column, would be wrong by exactly that offset — and would
look perfectly well behaved while being wrong, the same failure mode as handing the driver
J2000 coordinates instead of topocentric.

Files written from 2026-09-17 on carry two extra columns at the end,
**`ha_correction_hours`** and **`dec_correction_deg`**, holding the correction that was in
force when the row was written. So a later file is self-describing: add the value back to
`renishaw_ha_hours` to recover the uncorrected reading, or subtract it from an older file's
to bring the two onto the same footing.

They are per-row rather than a header line precisely so they survive concatenation, and
appended rather than inserted so the first 14 columns stay where they are. **The 2026-09-16
file below has neither column**, which itself means "no correction applied" — it is the only
file that will ever be in that state.

## Known gaps in the 2026-09-16 run

- **`through_pole` is 0 on all 16 rows** (max Dec 57.3°). That transformation has never run
  against real data.
- **Every point carries an unmeasured solve delay.** The encoder counts are read when the
  Action fires — after the exposure *and* after the solve — not at exposure midpoint. Gaps
  ran 3.4 to 77.5 minutes because the pointing error (31′) exceeded PinPoint's catalog
  region and every solve fell back to all-sky. Any tracking drift over that delay appears as
  common-mode scatter in both encoders, which is what the residual ~30″ looks like. Log the
  exposure midpoint before reading much into it.
