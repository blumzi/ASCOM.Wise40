---
name: focuser-com-interfaces
description: "Platform 6.5 SP1 defines no V1 IFocuser, so FocusMax v4 cannot bind and dies at startup while v5 works - and swapping the declared interface on a ClassInterfaceType.None driver REMOVES the old one from QueryInterface"
metadata:
  type: reference
---

## Use FocusMax v5, not v4

**v5 connects, jogs and sets position. v4 dies at startup with "Unexpected error; quitting."**
Established 2026-09-22, and **not** caused by anything in this repo - the driver was reverted to
its original state and v4 still failed while v5 still worked.

The likely reason is in the Platform, not in Wise40 code. Read from the installed assembly:

```
ASCOM.DeviceInterfaces 6.0.0.0  (Platform 6.5 SP1, 6.5.1.3234)
Focuser interfaces defined: IFocuserV2, IFocuserV3        <- no V1 IFocuser at all
```

There is **no V1 `IFocuser` interface to implement**, so a client old enough to early-bind to the
V1 focuser IID can never be satisfied by any driver on this Platform. `Link` is a V1-*shaped*
property, but it hangs off `IFocuserV2`; there is no V1 interface behind it. That fits the
evidence exactly - v4 fails at object creation, before any connect; v5 binds V2 and works.

Making v4 work would need a separate V1 shim ProgID or the Platform's own adapter. Not worth it.

## Do not swap the declared interface - ADD to it

`Focus/Driver.cs` is `[ClassInterface(ClassInterfaceType.None)]`, which means the COM class
exposes **exactly the interfaces it implements** - there is no auto-generated class interface.

So changing

```csharp
public class Focuser : IFocuserV2, IDisposable      // before
public class Focuser : IFocuserV3, IDisposable      // after
```

**removed IFocuserV2 from QueryInterface** and broke every V2 client at creation. Verified by
reflecting on the built assembly, which is the cheap way to check this:

```powershell
$t.GetInterfaces() | ForEach-Object { $_.Name }     # what QueryInterface will answer
```

If a V3 upgrade is ever wanted, declare **both** - `IFocuserV3, IFocuserV2`. They are
member-identical (45 each) and V3 does not inherit V2; they are parallel declarations, so one
implementation satisfies both and nothing new has to be written. V3 is a version marker.

Also keep `InterfaceVersion` in step with what is declared: a driver implementing V3 while
reporting 2 invites a client to negotiate down.

## Three real defects, deliberately left alone

The focuser works, so these were not worth a third speculative change to a live driver. Fix them
when there is a reason to touch it:

- **`Link` and `Connected` disagree** (`Focus/Driver.cs`). `Connected` is per-client -
  `_connected && wisefocuser.Connected` - while `Link` forwards straight to the singleton and
  never touches `_connected`. A client connecting via `Link` brings the hardware up but leaves
  this shim reporting disconnected. `Link` should simply delegate to the `Connected` property.
- **`WiseFocuser.Connected`'s setter has no try/catch** and assigns `_connected` *after* its
  connectables loop. Any throw partway leaves some connectables connected and the driver
  believing it is disconnected - which turns one connect failure into `NotConnectedException` on
  every later call, with nothing in the log to explain it.
- **`WiseFocuser.ReadProfile()` is an empty body** that still opens an `ASCOM.Utilities.Profile`.
  The focuser reads no settings at all, and has **no section in `Settings.json`** - the same
  missed-migration family as SafeToOperate had. See [[settings-live-in-json]].

## The process lesson

Two changes were made to this driver on a hypothesis, without ever seeing FocusMax's error text.
The first made things worse - from "starts, then throws on operations" to "will not start" - and
both were reverted. The error text, and which client version was in use, settled in one message
what two deploys had not.
