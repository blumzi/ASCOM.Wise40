---
name: wise40-filter-metadata-sources
description: "Wise40 no longer keeps its own filter inventory - where the names and offsets now come from, and the file paths that took work to find"
metadata: 
  node_type: memory
  type: reference
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-08-22T17:10:58.820Z
---

Decided and implemented 2026-08-22. Wise40 keeps **only the RFID tag** for each wheel position — it is read off the wheel and nothing else can know it. Everything else is looked up:

| what | where |
|---|---|
| filter names | **MaxIm DL**: `Documents\MaxIm DL 7\Settings\MaxIm CCD\SetupFilterWheel.txt` |
| focus offsets, ref/pointing filter, autofocus mags | **ACP**: `C:\Users\Public\Documents\ACP Config\FilterInfo.txt` |

The two CSVs Wise40 used to maintain are retired to `C:\Wise40\FilterWheel\retired-2026-08-22\`, and `IFilterWheel.Names` / `FocusOffsets` now report empty strings and zeros — one element per position, never a zero-length array, since ASCOM clients index them.

## Things that cost time to establish

**The observatory runs MaxIm DL 7**, and both 6 and 7 have a `SetupFilterWheel.txt`. Pick by **version order, newest first** — *not* by which file was written most recently. On the dev machine DL 6's file was a week newer than DL 7's, so "newest file" silently reads the wrong version.

**MaxIm keeps settings in files, not the registry** (vendor-confirmed). A registry sweep finds nothing filter-related; the `CurrentVersion` keys are empty.

**The slot keys are 1-based (`F1`..`F64`)** while our positions are 0-based, and the lines are **sorted as text** — `F1, F10, F11 … F2, F20` — so parsing by line order silently scrambles the mapping.

**`ASCOMFilter` in the key is the wheel *family*, not a constant.** A native driver would give `Camera0_ApogeeUSBFilterF1` and every cell would go blank. `Camera0_SelectedFilterWheelName` says which is active.

**MaxIm keeps one list per camera, not per wheel.** So the 8-position wheel takes slots 1–8 and the 4-position wheel slots 1–4, of the same list.

**ACP's `FilterInfo.txt` carries no names at all** — it is positional, `offset,ref-filt,ptg-filt[,af-minmag,af-maxmag]`, and what follows the `;` is a comment. Ours are placeholders (`c1`..`c8`).

## Why the dialog shows a reason instead of a blank

Empty cells were indistinguishable from an empty slot, MaxIm not being set up, and us looking in the wrong directory — which turned out to be the actual cause. See [[wise40-build-and-environment-gotchas]] for the `CreateProcessAsUser` defect behind it.
