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

## Fixing the first exception just moved the failure four lines down

Measured on the sky 2026-10-11 by pointing `TessW_IPAddress` at an unused address on the subnet
and cycling the chain — the faithful stand-in for power-off, since ARP fails either way.

With the `KeyNotFoundException` fixed, the hub passes NaN through cleanly
(`observingconditions/0/skytemperature` → `ErrorNumber=0, Value=NaN`) and the **next** line throws
instead: `FromOCHProperty` computes `DateTime.Now.Subtract(TimeSpan.FromSeconds(age))`, and a
station that has never answered reports an age of ~6.4e10 s — about **2030 years**, because
`_lastDataFetch` is still `DateTime.MinValue`. That lands before year 1, so `DateTime.Subtract`
throws **`ArgumentOutOfRangeException`**, which the guard sitting right there did not catch
because it only caught `OverflowException`.

Logged 8 times as `Digest: cannot digest hub property "SkyTemperature": caught The added or
subtracted value results in an un-representable DateTime.`

**So the per-field guard in `Digest` was not belt-and-braces — it is what kept the document alive
on the day.** Two different exceptions, four lines apart, in the same dead-station path. Expect a
third: when a value is absent, every derived quantity computed from it is suspect, not just the
value.

The other five sensors compute the same age and did **not** throw (zero `onTimer ... Caught` lines
in the whole log), so this is one site, not a pattern — `och.TimeSinceLastUpdate` evidently answers
differently for `CloudCover` than for `SkyTemperature`.

## Fixed, and the shape of the fix

1. `WiseTessW.SensorValue(key)` → NaN when the key is absent.
2. `FromOCHProperty` catches per property — this also covers `WiseSite.och` being **null**, which
   it is whenever the hub was unreachable at site initialisation.
3. `FromSensor` tolerates a null `LatestReading`; it is null for every `SingleReading` sensor until
   the first reading arrives, so a sensor that throws every cycle NRE'd here forever.
4. `Digest` is assembled field by field, and an unproducible field becomes
   `SensorDigest.Unreadable(name, affectsSafety)` — never null, because every consumer
   dereferences these without a null check.
5. The age guard in `FromOCHProperty` catches `ArgumentException` as well as `OverflowException`,
   so an age that cannot be represented becomes a missing *timestamp* rather than a missing field.

Verified live: with the station unreachable, `status` returns a complete document — SkyTemperature
`***` and stale, Clouds unsafe and naming itself in `UnsafeReasons`, and every VantagePro field
still carrying live values.

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
