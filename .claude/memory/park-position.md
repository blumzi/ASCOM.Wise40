---
name: park-position
description: "The Wise40 park position is Dec 66 on the meridian - what it looks like physically, why Park() lands about 15 arcmin west of it, and why commanding HA instead is NOT the fix"
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

### DO NOT command HA to fix this — it was tried and it ran the axis into a limit

This section used to say the proper fix was to point `Park()` and `ParkFromGui` at
`SlewToHaDecAsync(0, 66)`, since an hour angle is time-independent and needs no lead. **The
reasoning is still right and the implementation is broken.** It was done in PR #33, and on
2026-09-19 Park drove the primary axis about 80° the wrong way into a physical limit switch,
because `Angle.ShortestDistance` mis-computes distance and direction for HA-typed angles: it
reported 5.35 rad for a 0.357 rad move and the value grew as the axis ran. Reverted in PR #40,
and `SlewToHaDecAsync` now throws rather than slewing.

See [[ha-angle-distance-is-broken]]. **Fix the arithmetic, with a test, before revisiting this.**
Until then the lead below is not a workaround, it is the only method that works.

### The method that works: lead the RA target

An HA target is unusable ([[ha-angle-distance-is-broken]]), so RA is the only route. Target `RA = LST_now + d`, where `d` is the expected slew duration in hours. Confirmed — a trim with `d = 0.00417` h (15 s) landed **25″** off, against 310″ without.

Iterating unled trims does *not* converge, because each reintroduces its own duration: a second unled trim took 15.4 s and put back 231″ of the 310″ it had just removed. `d` need not be accurate; any reasonable estimate shrinks the error to the difference between guess and actual, so one led pass reaches tens of arcsec.

### Why not Alt/Az

Asked and rejected 2026-09-18. Both HA/Dec and Alt/Az are time-independent, so fixing the drift is *not* the deciding factor — these are:

- **HA/Dec *is* the axis position** on an equatorial: primary on the meridian, secondary at 66°. Alt/Az needs a latitude-dependent spherical conversion to get back to what the motors actually do.
- **An Alt/Az park moves when the site does.** Correct the site latitude — plausible after the pointing-model work — and it commands a different mechanical position though nothing physical moved. HA/Dec is immune.
- **Refraction** is applied to Alt/Az by ASCOM convention. If that is ever switched on, a "fixed" Alt/Az park becomes weather-dependent. HA/Dec is a pure mechanical statement.
- **Alt/Az → HA/Dec has two solutions** in general, east and west of the meridian. Unique at Az 0 exactly, but fragile to rely on, and ill-conditioned near zenith.
- **Four of the five soft limits are already in HA/Dec** (`eastern_haLimit`, `western_haLimit`, `lower_decLimit`, `upper_decLimit`); only `altLimit` is in Alt, so an HA/Dec park can be checked against them by inspection.

Alt/Az would be the better *description* if the park exists for physical clearance — tube off the dome slit, or positioned for a cover. **Nobody recorded why 66°**; there is no comment at `WiseTele.cs:528`. Worth recovering, because it decides whether 66 is a constraint or an arbitrary choice. Even then: convert once, store HA/Dec.

## Leave tracking off once parked

With tracking off the axis holds still and the **mechanical** hour angle is frozen, so the telescope stays on the meridian. With tracking on it holds RA instead and drifts west at sidereal. See [[stop-tracking-after-tests]].

Related: [[ha-angle-distance-is-broken]] — why the Action that would express this target
directly is disabled, and why the RA/Dec path plus a lead is the only working route.
