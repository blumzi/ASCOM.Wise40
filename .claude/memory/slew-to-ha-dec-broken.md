---
name: slew-to-ha-dec-broken
description: "SUPERSEDED - the slew-to-ha-dec parsing bugs are fixed, but the Action is now disabled because HA-typed slews mis-compute distance; kept for the parsing detail"
metadata:
  type: project
---

**Superseded 2026-09-19. The parsing bugs described here are FIXED; the Action is now
deliberately disabled for a worse reason** — an HA-typed slew mis-computes its distance and ran
the primary axis into a limit switch. See [[ha-angle-distance-is-broken]], which is the note to
read. This one is kept for the parsing detail and the diagnostic trap at the end.

The original entry follows.

**`Action("slew-to-ha-dec", ...)` could never succeed.** Found 2026-09-18, confirmed against the
live driver.

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

**~~Fix `Park()` in the same change~~ — DO NOT.** This used to advise pointing `Park()` and `ParkFromGui` at HA/Dec, on the grounds that an HA target is time-independent and needs no slew-duration lead. That was done in PR #33 and drove the primary axis about 80° the wrong way into a limit switch on 2026-09-19; both were reverted to right-ascension targets in PR #40. The reasoning was sound, the arithmetic underneath it is not. See [[ha-angle-distance-is-broken]].

**Diagnostic note for next time:** the Alpaca `action` endpoint needs `PUT`, not `GET`, and the parameter must be URL-encoded (`curl --data-urlencode`) or the embedded `=` and `,` are mangled and you get the misleading "Two parameters needed" instead.
