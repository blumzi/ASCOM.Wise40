---
name: park-position
description: "The Wise40 park position is Dec 66 on the meridian - what it looks like physically, and why Park() lands about 15 arcmin west of it"
metadata:
  type: reference
---

**The park position is declination +66°, on the meridian (HA 0).** Confirmed by Arie 2026-09-18. It is already what the code does: `parkingDeclination = Angle.DecFromDegrees(66.0)` (`WiseTele.cs:528`) and `parkingRa = wisesite.LocalSiderealTime` (`:1996` in `Park`, `:2118` in `ParkFromGui`).

**What it looks like on the sky**, measured rather than computed — useful for recognising a parked telescope or sanity-checking a park:

| | |
|---|---|
| Dec | +66.0007° |
| HA | −25″ |
| **Altitude** | **54.608°** |
| **Azimuth** | **0.004°** (due north) |

## Park() lands about 15 arcmin west of the meridian

`parkingRa` is sampled **once, before the slew starts**, and never updated. LST keeps advancing while the mount moves, so the telescope arrives west of the meridian by exactly the slew duration:

- a **62 s** slew ⇒ LST advances 0.01731 h ⇒ **934″ = 15.6′ west**
- measured on 2026-09-18: a 62.3 s slew with a 0.012 h lead applied still ended +310″ west, matching the arithmetic

The sign is worth being sure of: HA = LST − RA, and RA is held fixed, so HA grows **positive** = **west**.

**The fix is a lead**, not a loop: target `RA = LST_now + d`, where `d` is the expected slew duration in hours. Confirmed to work — a trim with `d = 0.00417` h (15 s) landed **25″** off, against 310″ without. Iterating unled trims does *not* converge, because each trim reintroduces its own duration; a second unled trim took 15.4 s and put back 231″ of the 310″ it had just removed.

`d` need not be accurate. Any reasonable estimate shrinks the error to the difference between guess and actual, so one led pass gets to tens of arcsec.

## Leave tracking off once parked

With tracking off the axis holds still and the **mechanical** hour angle is frozen, so the telescope stays on the meridian. With tracking on it holds RA instead and drifts west at sidereal. See [[stop-tracking-after-tests]].

Related: [[slew-to-ha-dec-broken]] — the Action that would express this target directly is broken, which is why the RA/Dec path plus a lead is the working route today.
