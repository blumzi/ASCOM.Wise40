# ASCOM.Wise40

Driver suite for the Wise Observatory 40-inch telescope.

## Where Claude keeps its notes

Both live in this repo and are git-tracked on purpose, so they version alongside the code they
describe and survive a machine rebuild.

- `.claude/memory/` — durable findings about this observatory: hardware facts, traps that cost
  real time, and decisions with their reasoning. One fact per file.
  `.claude/memory/MEMORY.md` is the index.
- `.claude/plans/` — implementation plans.

Create further subfolders under `.claude/` as suitable.

**Read `.claude/memory/MEMORY.md` at the start of a session** and open whichever entries look
relevant. They are point-in-time observations, not live state: if one cites a file, a line
number or a flag, verify it still exists before relying on it.

## Memory index

- [Renishaw encoder calibration](.claude/memory/renishaw-encoder-calibration.md) — calibrated against the sky 2026-09-16; no scale error, zero point corrected, now the encoders in use
- [ACP / driver integration](.claude/memory/wise40-acp-driver-integration.md) — ACP will not pass `Action` through, `DriverAccess` is not COM-creatable, and only one ProgID is safe to use
- [Build and environment gotchas](.claude/memory/wise40-build-and-environment-gotchas.md) — COM registration needs elevation, an elevated rebuild wipes ASCOM Profile values, the watcher gives children a broken environment, logs roll at noon UT
- [Driver Connected/Dispose scoping](.claude/memory/wise40-driver-connected-dispose.md) — per-client teardown used to reach shared singletons and stop the mount; fixed in six drivers, two left
- [Filter metadata sources](.claude/memory/wise40-filter-metadata-sources.md) — names from MaxIm DL 7, offsets from ACP; Wise40 keeps only the RFID tags
- [Filter wheel Arduino source](.claude/memory/wise40-filterwheel-arduino-source.md) — three copies exist; the live one is under Documents\Arduino, not in this tree
- [Filter wheel firmware validated](.claude/memory/filterwheel-fixes-awaiting-hardware-test.md) — works end to end as of 2026-08-15; the slit detector needs ~40 ms to settle or it lies convincingly
- [Filter wheel RFID stays on SoftwareSerial](.claude/memory/filterwheel-rfid-stays-on-softwareserial.md) — rewiring D0 to pin 19 was considered and rejected; do not propose it again

## Building

`RegisterForComInterop` is on for several projects, so **build elevated**. A non-elevated build
unregisters and then fails to re-register, and re-registration wipes the driver's ASCOM Profile
subkey. Before debugging any setting that appears to have "reverted on its own", read the
build-and-environment memory.
