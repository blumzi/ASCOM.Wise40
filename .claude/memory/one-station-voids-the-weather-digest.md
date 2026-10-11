---
name: one-station-voids-the-weather-digest
description: "A single unreadable sensor used to void the entire SafeToOperate status digest - a powered-down TessW blanked every weather field and stopped the observatory monitor's safety evaluation"
metadata:
  type: reference
---

**The weather subsystem has one chokepoint: `SafeToOperate.Action("status")`.** Everything
outside the driver — the Dash weather panel, `ObservatoryMonitor.CheckSituation()`, the REST
clients — learns the weather *and the safety decision* from that one JSON document. Until
2026-10-11 it was built in a single object initializer, so **any** sensor that threw took the
whole document with it.

## What a powered-down TessW did

`WiseTessW.sensorData` is populated only by a successful fetch. Powered down before the driver
started, it stays **empty**, and `SkyTemperature`/`Temperature` did a bare `sensorData["tempSky"]`
→ `KeyNotFoundException`. (`CloudCover` was already guarded and returned NaN, which is why cloud
cover degraded gracefully and sky temperature detonated.)

From there: OCH serves `SkyTemperature` from the TessW → `SensorDigest.FromOCHProperty` read it
unguarded → the `Digest` initializer threw → `Action("status")` threw. Result:

- **Dash** blanked *every* weather label to `***` — temperature, humidity, pressure, wind, rain,
  dew point, sun elevation — including the ones from a perfectly healthy VantagePro.
- **ObservatoryMonitor** caught it and `return`ed out of `CheckSituation()` **on every cycle**, so
  the watchdog stopped evaluating safety altogether. It fails open, not closed.

## Why it looks intermittent

Once the station has answered **once**, the stale values stay in the dictionary forever, so a
later power-down never throws — it serves old numbers and reports staleness correctly. The hard
failure needs *power off across a driver start*, which is why it reads as "it worked yesterday".

## Fixed, and the shape of the fix

1. `WiseTessW.SensorValue(key)` → NaN when the key is absent.
2. `FromOCHProperty` catches per property — this also covers `WiseSite.och` being **null**, which
   it is whenever the hub was unreachable at site initialisation.
3. `FromSensor` tolerates a null `LatestReading`; it is null for every `SingleReading` sensor until
   the first reading arrives, so a sensor that throws every cycle NRE'd here forever.
4. `Digest` is assembled field by field, and an unproducible field becomes
   `SensorDigest.Unreadable(name, affectsSafety)` — never null, because every consumer
   dereferences these without a null check.

**`Unreadable` is deliberately asymmetric: unreadable means NOT safe for a sensor that affects
safety, and merely "no value" for an information-only one.** Fail safe where it counts, stay quiet
where it does not.

## How to apply this

When adding a field to any `*Digest`, assume the source can throw and give it its own guard. An
aggregate that one contributor can void is a subsystem-wide single point of failure wearing the
costume of a status report. Same reasoning as [[diagnostics-that-break-what-they-measure]]:
suspect the reporting path before concluding the hardware is the problem.

Still unfixed, deliberately out of scope: `TessWRefresher.Digest()` calls
`tessw.Action("status", "")`, which the TessW driver does not support (`OCHTag` and `raw-data`
only), so it would throw `ActionNotImplementedException`. Nothing calls it today.
