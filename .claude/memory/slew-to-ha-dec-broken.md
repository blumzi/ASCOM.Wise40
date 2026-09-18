---
name: slew-to-ha-dec-broken
description: "The slew-to-ha-dec Action always fails on a case-sensitivity bug, which also kills the Dash's HA/Dec slew - to be fixed"
metadata:
  type: project
---

**`Action("slew-to-ha-dec", ...)` can never succeed.** Found 2026-09-18, confirmed against the live driver, **not yet fixed** — Arie asked to leave it for later.

`WiseTele.cs:4093` lowercases the whole parameter string:

```csharp
List<string> par = parameter.ToLower().Split(',').ToList();
```

and then `:4101` / `:4104` test it case-sensitively:

```csharp
if (p.StartsWith("HourAngle="))        ...
else if (p.StartsWith("Declination"))  ...
```

Neither can ever match lowercased text, so `ha` and `dec` stay `NaN` and the action returns **"Parameters HourAngle and Declination must be supplied"** for every well-formed call.

**It takes the Dash down with it.** `Dash.cs:1170` calls

```csharp
wiseTelescope.Action("slew-to-ha-dec", $"HourAngle={ha},Declination={dec}");
```

so the Dash's HA/Dec slew has presumably never worked.

The parsing fix is two lines — compare against lowercase, or drop the `ToLower()` and parse case-insensitively.

**That alone is not enough.** `MoveToKnownHaDec` behind it does

```csharp
Angle ra = wisesite.LocalSiderealTime - ha;
SlewToCoordinatesAsync(ra.Hours, dec.Degrees, op, false);
```

— it converts to RA **once, at call time**, then slews to that fixed RA. So it inherits exactly the drift described in [[park-position]]: the result lands west of the requested hour angle by the slew duration. A repaired-but-unchanged `slew-to-ha-dec` would look like it works and be quietly wrong.

A true HA slew looks feasible without much work: `CurrentPosition` already handles `Angle.AngleType.HA` (returning `HourAngle`), and `ScopeAxisSlewer` branches on `primaryAngleType`, so an HA-typed target would have the loop compare encoder HA against a fixed HA — no drift and no lead. **Verify that path before relying on it**; it has never been exercised.

**Fix `Park()` in the same change.** `Park()` and `ParkFromGui` command `RA = LST` (`WiseTele.cs:1996`, `:2118`), which lands ~15′ west of the meridian because RA drifts relative to the mount while it slews. Once this Action works, both should command **HA/Dec** instead — an HA target is time-independent, so that removes the error rather than compensating for it and **no slew-duration lead is needed**. See [[park-position]], which also records why Alt/Az was considered and rejected.

**Diagnostic note for next time:** the Alpaca `action` endpoint needs `PUT`, not `GET`, and the parameter must be URL-encoded (`curl --data-urlencode`) or the embedded `=` and `,` are mangled and you get the misleading "Two parameters needed" instead.
