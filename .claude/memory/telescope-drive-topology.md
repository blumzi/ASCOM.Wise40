---
name: telescope-drive-topology
description: "How the Wise40 analog drive is actually wired - TeleSlew is a shared speed selector, so both axes must use the same speed among slew/set; only guide is independent"
metadata:
  type: reference
---

Told by Arie 2026-09-17 and confirmed against `Telescope/WiseMotor.cs:100-153` and `WiseTele.cs:539-552`. **Not derivable from the code alone** — the code reads as though each motor were independent.

The drive is fully analog with constant-speed motors: **4 on HA** (slew, set, guide, track) and **3 on Dec** (slew, set, guide). No variable speed, so no PID is possible.

## The wiring that matters

`TeleSlew` is **not** a motor enable. It is a **shared speed selector** for the `Tele<direction>` pins:

| pins energised | result |
|---|---|
| `TeleSlew` + `Tele<dir>` | that axis moves at **slew** speed |
| `Tele<dir>` alone | that axis moves at **set** speed |
| `Tele<dir>Guide` | that axis moves at **guide** speed, independent of `TeleSlew` |

All four direction motors share the one `SlewPin` (`WiseTele.cs:539-552` passes the same `SlewPin` to North/South/East/West).

**Consequences:**

1. If either axis is moving at slew or set, the other can only use **the same** of those two.
2. One axis at slew/set **and** the other at guide is fine — guide has its own pins.

## Why the ReadyToSlewFlags rendezvous is load-bearing

`ReadyToSlewFlags` (`WiseTele.cs:2125-2150`) holds both axes in lockstep at every rate. It is tempting to relax that as pointless coupling — RA sits idle 56% of every slew waiting for Dec. **Do not relax it at `set`.**

`WiseVirtualMotor.SetOn` (`WiseMotor.cs:110-131`) only declines to turn `SlewPin` *off* when the other axis is at `rateSlew`; it then energises the direction pin regardless of rate. So an axis asking for `rateSet` while the other is at `rateSlew` would run with `SlewPin` still on — **physically moving at slew speed while the control loop believes it is at set speed**, 1.69°/s against an assumed 49.6″/s, a factor of 123, with `stopMovement` sized for the slow rate. The rendezvous is the only thing preventing that.

**Only the `guide` rendezvous is safe to remove**, since guide uses separate pins and is unaffected by `TeleSlew`.

## Rates

Nominal (`Common/Const.cs:23-29`) against measured from 2026-09-16 logs:

| rate | nominal | RA measured | Dec measured |
|---|---|---|---|
| slew | 2.0 °/s | 1.88 | 1.69 |
| set | 60 ″/s | 52.3 | 49.6 |
| guide | 1.0 ″/s | 0.86 | 0.79 |
| track | 15.04 ″/s | — | n/a (HA only) |

`rateTrack` is **not** in the slew cascade (`WiseTele.cs:176`), so the 4th HA motor never participates in a slew.

See [[slew-time-where-it-goes]] for what this costs in practice.
