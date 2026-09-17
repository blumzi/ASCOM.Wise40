---
name: filterwheel-rfid-stays-on-softwareserial
description: "Decision not to rewire the filter wheel's RFID reader onto hardware Serial1 - SoftwareSerial on pin 52 is permanent"
metadata: 
  node_type: memory
  type: project
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-08-07T12:32:19.623Z
---

The Wise40 filter wheel's ID-12LA RFID reader stays on **SoftwareSerial, pin 52** (`USE_SOFTWARE_SERIAL` in `Id12la.h`). Peter decided on 2026-08-07 **not** to rewire the reader's D0 to pin 19 to use the Mega's hardware `Serial1`. Don't propose it again.

**Why:** It is a soldering job on installed dome hardware, not a code change. The theoretical gain — SoftwareSerial masks interrupts while receiving a byte, alongside a 57600 baud host port, which can produce intermittent framing errors — did not justify touching the wiring.

**How to apply:** The `#else` branches guarded by `USE_SOFTWARE_SERIAL` (in `Id12la.h`, `Id12la.cpp` and `FilterWheel_Arduino.ino`) are therefore dead code, kept but never compiled. If intermittent RFID framing errors ever show up, suspect SoftwareSerial timing before suspecting the tag or the reader — the framing is now checked properly and reports `error:timeout` / `error:noise` / `error:no ETX` rather than corrupting memory. See [[wise40-filterwheel-arduino-source]] and [[filterwheel-fixes-awaiting-hardware-test]].
