---
name: wise40-filterwheel-arduino-source
description: Which of the three FilterWheel Arduino sketch copies on this machine is the live one for the Wise40 filter wheel
metadata: 
  node_type: memory
  type: project
  originSessionId: 94e8e821-0e9f-4974-a4e4-dbf54d206461
  modified: 2026-08-07T11:20:03.811Z
---

The live Arduino source for the Wise40 filter wheel is `C:\Users\mizpe\Documents\Arduino\FilterWheel_Arduino` (git repo, remote `https://github.com/blumzi/FilterWheel_Arduino.git`, `master` @ `eb69ce6` "Final cleanup", 2019-07-01). Two other copies on disk are NOT in use:

- `C:\Users\mizpe\source\repos\ASCOM.Wise40\FilterWheelArduino` — legacy 2017 sketch (note: no underscore in the name). Stepper-only, 9600 baud, single-character commands (`'1'`, `'A'`, `'W'`, `'I'`), no RFID. Cannot talk to the current driver.
- `C:\Users\mizpe\Documents\Arduino\FilterWheel_Arduino-local` — abandoned Feb/Mar 2019 snapshot, not a git repo, predates the `error:connector:` and `error:no-slit` reporting the driver parses.

**Why:** The wrong copy looks authoritative — `FilterWheelArduino` sits right inside the `ASCOM.Wise40` solution tree, and `Wise40.sln`'s reference to the correct project uses a stale relative path (`..\..\..\Arduino\FilterWheel_Arduino\...`, which resolves to the nonexistent `C:\Users\mizpe\Arduino` since the repo moved to `source\repos`). Peter declined to fix that path on 2026-08-07.

**How to apply:** Edit filter wheel firmware only in `Documents\Arduino\FilterWheel_Arduino`. Confirm by matching the serial protocol in `ASCOM.Wise40\FilterWheel\ArduinoInterface.cs` — 57600 baud, commands `get-tag` / `move-cw:N` / `move-ccw:N`, replies `tag:<tag>` and `error:connector:<n>`. RFID tags in the repo's `Tags.txt` match the hardcoded arrays in `FilterWheel\WiseFilterWheel.cs`. See [[wise40-repo-layout]].
