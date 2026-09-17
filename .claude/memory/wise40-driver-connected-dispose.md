---
name: wise40-driver-connected-dispose
description: "Wise40 drivers forwarded per-client Connected/Dispose to shared singletons - what that broke, what the fix looks like, and which drivers still have it"
metadata: 
  node_type: memory
  type: project
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-08-31T14:43:28.048Z
---

Fixed 2026-08-31, commit `567c81b`, built elevated and pushed. Every Wise40 driver is a per-client COM shim (`<Project>/Driver.cs`) over a singleton, and the shims forwarded **both** lifecycle calls straight through. So any client doing what ASCOM clients normally do on exit — `Connected = false`, then `Dispose()` — tore down hardware the other clients were still using.

**This fired in production.** `ObservatoryMonitor.CloseConnections()` holds `DriverAccess` handles to Telescope, Dome and SafetyMonitor and does exactly that sequence on all three.

## The fix, now uniform across six drivers

```csharp
get { return _connected && <singleton>.Connected; }
set {
    if (value == _connected) return;
    if (value) <singleton>.Connected = true;   // idempotent; first client wins
    _connected = value;                        // never propagate false
}
```

Hardware comes up on the first connect and **stays up for the life of the local server**. Nothing disconnects it on a client's behalf — `WisePin.Connect(false)` releases DAQ bit ownership, which is a server-lifetime concern. `Dispose` no longer forwards in Telescope, Dome, Focus.

Applied to: Telescope, Dome, Focus, FilterWheel, SafeToOperate, SafeToImage.

**Still unfixed (tier 2):** TessW and VantagePro stop weather polling on a client disconnect. Bounded — a stale reading, not a moving mount.

## What each one actually did

- **Dome** — `Dispose()` drove `leftPin.SetOff()`, `rightPin.SetOff()`, `Vent = false`, then set the **static** `wisedome` field to null, leaving every other instance holding nothing. Separately its `Connected` setter had the already-connected guard *below* the side effect, so any redundant connect could launch `StartFindingHome()` — fixed in `be4f755`.
- **Telescope** — `Connect(false)` over four direction motors, the tracking motor, both encoders, both axis monitors.
- **SafeToImage** — holds `WiseSafeToOperate.InstanceImage`, but `StopSensors()` and `_prioritizedSensors` are both **static**, so it stopped SafeToOperate's sensors too. Easy to under-rate; I did at first.
- **SafeToOperate** — its setter assigned `_connected = WiseSite.och.Connected` instead of `value`. `WiseSite` leaves `och` null when the hub is unreachable, so that threw `NullReferenceException`; and when the hub was merely disconnected, a *connect* silently became `StopSensors()` and reported success. Now throws `NotConnectedException`.

## The rule to carry forward

Keep the singletons — there really is one mount. The defect is a singleton whose lifecycle is driven by individual clients. Never let a per-client interface forward teardown to shared state; specifically, a process-lifetime singleton should not expose `IDisposable` through a COM interface.

Harmless, no change needed: ComputerControl, ClarityII, DavisVantage, SafetySwitch, MaintenanceSwitch, Boltwood, GlobalSafeToOperate. `Common/WiseSafeToOperate.cs` and the whole `SafeToOperate.sav/` directory are stale duplicates no project compiles.

See [[wise40-build-and-environment-gotchas]].
