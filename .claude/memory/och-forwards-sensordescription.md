---
name: och-forwards-sensordescription
description: "The ObservingConditions hub forwards SensorDescription to whichever device serves the property, which makes it an authoritative provenance channel - and it answers while the station is unreachable"
metadata:
  type: reference
---

**`och.SensorDescription(property)` is answered by the device the hub has assigned to that
property, not by the hub.** So it tells you *which station* a reading comes from, without keeping
a property-to-station table anywhere in our code that could drift out of step with the hub's
configuration.

Established 2026-10-11: asking the hub for every property returned `"SensorDescription - <prop>"`,
which is TessW's and VantagePro's stub format, while Boltwood's stub format is
`"<prop> Description"`. The format that came back identifies the device that answered — that is
how we know SkyTemperature is served by the TessW.

Each weather driver now returns **its own name** there — `"TessW"`, `"Davis"`, `"Boltwood"` — so
provenance reaches the Dash tooltips for free. Keep the `MethodNotImplementedException` branches:
a driver must still refuse the properties it does not serve, which is what makes the hub's routing
discoverable at all.

**It answers while the station is unreachable**, because it touches no hardware — verified with the
TessW pointed at an unused IP. That is exactly when a display most needs to name the station, so
do not be tempted to compute provenance from live data.

Querying is cheap but not free (a COM call per property), and it cannot change without restarting
the hub, so `SensorDigest.ProvenanceOf` caches it. **Failures are deliberately not cached** —
`WiseSite.och` is null until the hub connects, and a miss then must not be remembered for the life
of the process.

Related: [[one-station-voids-the-weather-digest]], which is why the digest tolerates a station that
is not answering in the first place.
