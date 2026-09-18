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

The fix is two lines — compare against lowercase, or drop the `ToLower()` and parse case-insensitively. `MoveToKnownHaDec` behind it is fine and already does the `RA = LST − HA` conversion.

While fixing, consider adding the slew-duration lead described in [[park-position]] — without it an HA-targeted slew lands west of where it was aimed, which is the same defect `Park()` has.

**Diagnostic note for next time:** the Alpaca `action` endpoint needs `PUT`, not `GET`, and the parameter must be URL-encoded (`curl --data-urlencode`) or the embedded `=` and `,` are mangled and you get the misleading "Two parameters needed" instead.
