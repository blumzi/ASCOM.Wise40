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

## Working on this repo

**Changes go through pull requests — do not commit to `master`.** Branch with a short
kebab-case name, keep one commit per concern, push the branch and open a PR with `gh pr
create`. If `gh` is unavailable, hand over
`https://github.com/blumzi/ASCOM.Wise40/compare/master...<branch>?expand=1` instead.

## Memory index

- [Work via pull requests](.claude/memory/work-via-pull-requests.md) — branch and open a PR; don't commit to master
- [Notes live in the repo](.claude/memory/notes-live-in-the-repo.md) — memories and plans belong under `.claude/`, git-tracked
- [Telescope drive topology](.claude/memory/telescope-drive-topology.md) — `TeleSlew` is a shared speed selector, so both axes must use the same speed among slew/set; only guide is independent. Read before touching the slew rendezvous
- [Slew time: where it goes](.claude/memory/slew-time-where-it-goes.md) — 20° Dec slew 125.7 s → 60.8 s on 2026-09-17, every gain from a constant nobody had measured. **Read the caveat: almost all of it is Dec, and the HA axis is largely unverified**
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
