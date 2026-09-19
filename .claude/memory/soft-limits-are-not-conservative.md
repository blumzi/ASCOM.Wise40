---
name: soft-limits-are-not-conservative
description: "A physical limit switch was reached at HA -6.7255 while eastern_haLimit was -7.0; the limits are now +/-6.5, but a fixed HA bound cannot protect the mount at all declinations"
metadata:
  type: reference
---

**The hour-angle soft limits do not bound the physical limits.** Demonstrated the hard way on
2026-09-19: the telescope reached a physical limit switch at **HA −6.7255 h, Dec 62.915,
Alt 22.32** — inside `eastern_haLimit`, which was then **−7.0 h**. The soft limit never fired;
the hardware stopped the mount.

**The limits were tightened to ±6.5 h in response** (Arie's decision, 2026-09-19), giving about
3.4° of margin at the one declination where the trip point is known. Read the caveat below before
treating that as solved.

## Why a fixed hour-angle bound cannot work

The limit switches sense **absolute orientation**, so their trip locus is a *curve* in
(HA, Dec). `eastern_haLimit` and `western_haLimit` are single numbers that ignore declination
entirely:

```csharp
public readonly Angle eastern_haLimit = Angle.HaFromHours(-6.5);   // was -7.0
public readonly Angle western_haLimit = Angle.HaFromHours(6.5);    // was  7.0
```

So there must exist declinations where a switch trips before the soft limit, and Dec 62.9 is one
of them. **±6.5 h is a smaller constant, not a fix.** The rest of the (HA, Dec) trip locus has
never been mapped, so whether 6.5 is conservative at other declinations is equally unknown. A real
fix makes the limit a function of declination.

**Do not treat "inside the soft limit" as safe.** It was assumed to be, including in an earlier note in
[[park-position]] observing that four of the five soft limits are expressible in HA/Dec and can
therefore be "checked by inspection". They can be checked, but passing that check does not mean
the position is reachable.

## What is known about the switches

Two distinct things, easily conflated:

- **Mercury switches** on the axes, which Arie describes as cutting power, and as *less*
  stringent than the soft limits.
- **`HardLimit`**, a separate physical limit switch. **It cuts motor power** — confirmed by Arie,
  2026-09-19. The driver also reads it as a single input pin (`¬HardLimit`, teleboard
  `SecondPortCH` bit 1, `DigitalPortDirection.DigitalIn`) via `HardLimitSensor`, but that reading
  is only *notification*: the interlock is in copper and acts whether or not software notices.

It is a **single bit**: it identifies neither axis nor direction. It is read live rather than
latched in software, so the *reading* clears as soon as the mount comes off the switch. Software
cannot bring that about: motor power is gone, and on top of that `HardLimit` carries
`Attribute.ForcesDecision`, so `wisesafetooperate` reports unsafe and `Tracking.set`, `MoveAxis`
and `Backoff` all refuse.

There *is* a way past it, recorded so nobody rediscovers it and thinks it is a good idea:
`MoveAxis`'s guard is `if (!IsSafeWithoutCheckingForShutdown() && !ShuttingDown &&
!BypassCoordinatesSafety)`, so `BypassCoordinatesSafety = true` skips it. That defeats a
hard-limit interlock. Don't.

## Nothing aborts an in-progress slew on a HardLimit trip

The safety decision containing `HardLimit` is consulted only at motion **start** —
`Tracking.set`, `MoveAxis`, `SlewToCoordinatesAsync`, `SlewToTargetAsync`. The one thing that
runs *during* motion, `SafetyMonitorTimer.SafetyChecker`, calls
`SafeAtCoordinates(current RA, current Dec)`, which tests `altLimit` and the HA/Dec bounds and
**never consults `wisesafetooperate`**.

**This is not the hole it looks like.** Since `HardLimit` cuts power, the abort is already
enforced in hardware and a software one would be belt-and-braces at best. What software cannot do
is *undo* it: the axis is de-powered, so there is nothing to command, and adding `HardLimit` to
`SafetyChecker` would only make the driver notice sooner. `Backoff` would be the wrong response
in any case — it drives at slew rate, and the one thing you must not do at a limit is guess a
direction.

Recovery is therefore **always** an on-site job, not a software gap to be closed.

## Evidence the hardware, not the software, stopped it

Worth keeping because it is the clearest signature of this condition. Before and after a manual
`AbortSlew`:

```
before:  HourAngle -6.7254972   Slewing true, SlewPin true, both SetPin true
after:   HourAngle -6.7255016   all pins clear
```

The axis was already stationary while the driver still had the relays energised. If you see
that combination — commanded but not moving — suspect a limit switch, not the drive.

See [[ha-angle-distance-is-broken]] for what drove it there.
