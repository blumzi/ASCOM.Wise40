---
name: wise40-acp-driver-integration
description: "How an ACP script reaches the Wise40 driver - ACP does not pass Action through, DriverAccess is not COM-creatable, and which ProgID is safe"
metadata: 
  node_type: memory
  type: reference
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-09-17T02:24:30.475Z
---

Established on the instrument, 2026-09-16/17.

## ACP's `Telescope` object does NOT pass `.Action()` through

Tested and confirmed. A script that needs a driver Action must create its own client. `AcpScripts\Wise40 Action Test.vbs` is the ten-second check to re-run after any ACP or driver upgrade.

## `ASCOM.DriverAccess.Telescope` is not a registered ProgID

`CreateObject("ASCOM.DriverAccess.Telescope")` fails with **"ActiveX component can't create object"** — it is a .NET wrapper for .NET clients, never COM-creatable from VBScript. This silently cost a whole calibration run: 16 points solved, none recorded.

Create the driver's own ProgID directly, no wrapper and no `.DriverID` step.

## Which ProgID — this one matters

| ProgID | Registration | |
|---|---|---|
| `ASCOM.AlpacaDynamic1.Telescope` | **LocalServer32** | **use this** — routes to the one running instance, the same one ACP uses |
| `ASCOM.Wise40.Telescope` | InprocServer32 | **never from ACP** — loads the driver *into ACP's process* and stands up a second `WiseTele` singleton contending for the same DAQ pins |

## Never touch `.Connected` or `.Dispose` from a script

See [[wise40-driver-connected-dispose]]. `Action()` never checks `Connected`, and if ACP is running the telescope is connected already. An early version of the test script set `Connected = False` on exit and disconnected the mount out from under ACP.

## ACP script mechanics

- ACP reloads a `.vbs` on every run — **no need to restart ACP** to pick up an edited script. Only `AcquireSupport.wsc` / `UserActions.wsc` would need re-registration.
- Scripts live in `C:\Program Files (x86)\ACP Obs Control\Scripts\` — copying there needs elevation.
- Prefs are read in `SUP.Initialize()` at script start, so change them before hitting Run, not during.
- **UserActions hooks are synchronous in-process callbacks**, not separate scripts. Abortability is declared once, `Util.Abortable = True` in `AcquireSupport.InitializeCommon()`, and only makes blocking `Util` waits interruptible — a synchronous COM call cannot be preempted. So a hook that blocks makes Stop look dead. The documented way for hook code to stop a run is its return value (`TargetStart` → `False` terminates, `2` skips).
- A running log file reports **0 bytes** because ACP holds it open; the content is there.
