---
name: soft-limits-are-not-conservative
description: "A physical limit switch was reached at HA -6.7255 while eastern_haLimit is -7.0, so the fixed HA soft limits do not protect the mount at all declinations"
metadata:
  type: reference
---

**The hour-angle soft limits do not bound the physical limits.** Demonstrated the hard way on
2026-09-19: the telescope reached a physical limit switch at **HA −6.7255 h, Dec 62.915,
Alt 22.32** — inside `eastern_haLimit` of **−7.0 h**. The soft limit never fired; the hardware
stopped the mount.

## Why a fixed hour-angle bound cannot work

The limit switches sense **absolute orientation**, so their trip locus is a *curve* in
(HA, Dec). `eastern_haLimit` and `western_haLimit` are single numbers that ignore declination
entirely:

```csharp
public readonly Angle eastern_haLimit = Angle.HaFromHours(-7.0);
public readonly Angle western_haLimit = Angle.HaFromHours(7.0);
```

So there must exist declinations where a switch trips before ±7 h, and Dec 62.9 is one of them.
Whether ±7 h is conservative at *other* declinations is unknown — nobody has mapped it.

**Do not treat "inside ±7 h" as safe.** It was assumed to be, including in an earlier note in
[[park-position]] observing that four of the five soft limits are expressible in HA/Dec and can
therefore be "checked by inspection". They can be checked, but passing that check does not mean
the position is reachable.

## What is known about the switches

Two distinct things, easily conflated:

- **Mercury switches** on the axes, which Arie describes as cutting power, and as *less*
  stringent than the soft limits.
- **`HardLimit`**, a separate physical limit switch, read by the driver as a single input pin
  (`¬HardLimit`, teleboard `SecondPortCH` bit 1, `DigitalPortDirection.DigitalIn`) via
  `HardLimitSensor`. **Whether this one also breaks the motor circuit is not established** —
  the source cannot say, and it should not be assumed either way. Arie's intent was that it
  trigger an abort.

It is a **single bit**: it identifies neither axis nor direction. It is read live rather than
latched in software, so it clears as soon as the mount comes off the switch — but no software
motion path can achieve that, because `HardLimit` carries `Attribute.ForcesDecision`, so
`wisesafetooperate` reports unsafe and `Tracking.set`, `MoveAxis` and `Backoff` all refuse.
**Recovery is an on-site job.**

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

Whether that is a hole or merely redundant depends on the wiring question above. If the switch
cuts power, a software abort is belt-and-braces; if it does not, the driver will keep driving
into the stop. Adding it would be cheap — `SafetyChecker` already runs at 1 Hz with
`AbortSlew`/`Stop`/`Backoff` wired up — but `Backoff` is the wrong response to a hard limit and
would need thought. **Settle the wiring before designing it.**

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
