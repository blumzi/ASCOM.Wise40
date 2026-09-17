---
name: filterwheel-fixes-awaiting-hardware-test
description: "Wise40 filter wheel firmware - validated on the instrument 2026-08-14/15, and the detector characteristic that made it hard"
metadata: 
  node_type: memory
  type: project
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-08-22T17:11:34.982Z
---

The Wise40 filter wheel firmware works end to end as of 2026-08-15: `get-tag`, `move-cw:N` and `move-ccw:N` all walk the wheel and return tags matching `Tags.txt`. `master` is pushed in both repos. The telescope carries the **4-position wheel** (tags `7F001B4C16`, `7C0055F4EB`, `7F001B0573`, `7F000AF9A0`).

**The thing to carry forward:** the H21B1 slit detector needs **~40 ms to settle**. Sampled faster it keeps reporting the slit for ~7 steps after the wheel has left it, and at ~6 ms it renders the slit as `XX.XXXXX.XX` — eleven steps with two phantom occlusions — instead of the solid twelve it really is.

**Why that is a trap:** the false reading repeats *exactly*, pass after pass, because the sampling interval repeats. Repeatability looked like proof the gaps were physical; it wasn't, and it cost a wrong diagnosis. `centerOnSlit()` reports an "occluded" count as a sentinel — if it is ever non-zero, `SLIT_SETTLE_MILLIS` has become too short again.

**How to diagnose:** never conclude anything about the detector from readings taken at stepper speed (~1 ms/step). Use the `rfid` and `detect-slit` commands over serial; `detect-slit` powers the IR emitter itself. Commands now have long forms (`toggle-led`, `detect-slit`, `detect-connectors`) with the short ones kept for the driver.

**Two of three faults that session were hardware, not code:** the stepper's power supply was off, and one broken conductor on the reader's D0 line — found because an `rfid` probe reported *0 edges in 73,000 samples* while TIR tracked a tag correctly, which separated wiring from software in one run.

**Reads no longer move the wheel.** The host polls `get-tag` every 30 s; centring on that poll jiggled the wheel mid-exposure. Searching for the slit still always centres; reading the tag where it already is does not.

See [[wise40-filterwheel-arduino-source]] and [[filterwheel-rfid-stays-on-softwareserial]].
