---
name: wise40-build-and-environment-gotchas
description: "Wise40 traps that each cost real time - COM registration needing elevation, the watcher's broken child-process environment, log rollover, and known-bad startup paths"
metadata: 
  node_type: memory
  type: reference
  originSessionId: d2d2573a-1ddd-4a94-9e8c-408214927135
  modified: 2026-09-17T02:24:15.420Z
---

## Building

`RegisterForComInterop` is on for several projects, so a **non-elevated build unregisters and then fails to re-register**, leaving the driver missing from the ASCOM Profile. That is exactly how `ASCOM.Wise40.TessW.ObservingConditions` lost its registration — and it cost **3m44s of every startup** until fixed with an elevated `regasm /codebase`. Build from an elevated Visual Studio, or expect it back.

To compile-check without elevation, **specify the platform**:

```
MSBuild <proj> /t:Build /p:Configuration=Debug /p:Platform=x86 \
        /p:RegisterForComInterop=false /p:BuildProjectReferences=false
```

`/p:Platform=x86` is **not optional**. Without it MSBuild builds AnyCPU into `bin\Debug`, reports success, and leaves `bin\x86\Debug` — which is what the chain actually loads — untouched. The giveaway is the `Telescope -> ...\bin\Debug\...` line in normal verbosity.

`/p:BuildProjectReferences=false` compiles against the DLLs already on disk. Fast, but it will compile against **stale** dependencies: after making a constant `public` in `Hardware`, the Telescope build failed with `CS0117 ... does not contain a definition for` until `Hardware` was rebuilt first. Build `Common` → `Hardware` → `Telescope` in order. **That flag does not propagate to project references** — MSBuild walks into them and tries to unregister, which is how TessW lost its registration a second time on 2026-09-16. Add `/p:BuildProjectReferences=false` to compile one project against the DLLs already on disk.

### Overriding `OutputPath` breaks reference resolution

A compile-check that writes somewhere harmless looks like the obvious move, but
`/p:OutputPath=<scratch>` also becomes where MSBuild *looks for* the project references, so
every one of them fails:

```
CSC : error CS0006: Metadata file '<scratch>\Common.dll' could not be found
CSC : error CS0006: Metadata file '<scratch>\Hardware.dll' could not be found
```

Stage the existing binaries into the scratch directory first, then build:

```powershell
Copy-Item "<repo>\Telescope\bin\x86\Debug\*" $scratch -Recurse -Force
```

Cost this twice on 2026-09-18 before the pattern was obvious.

### Elevation is needed at EVERY step, which is why Claude cannot deploy

Three things in the cycle need an administrator token:

| step | why |
|---|---|
| stop the chain | the service ACL denies `WP` to Interactive Users — see below |
| build the solution | `RegisterForComInterop` re-registration, per the top of this section |
| start the chain | the same ACL denies `RP` |

And the build cannot run *while* the chain is up, because the RemoteServer holds
`ASCOM.Wise40.Telescope.dll` open. So there is no ordering that avoids elevation, and
`Start-Process -Verb RunAs` raises a UAC prompt that a tool call cannot answer.

**The workable division of labour:** Claude writes the change, compile-checks it
non-elevated (above), opens the PR, and afterwards verifies the deploy and runs the on-sky
tests through the Alpaca API. Arie does the elevated build and the chain restart. Do not plan
a workflow that assumes Claude can rebuild and retest unattended — it cannot, and proposing
one just wastes a round trip.

**Verifying someone else's build**, which Claude *can* do unaided:

- the x86 DLL timestamp under `Telescope\bin\x86\Debug`, against `bin\Debug` — if AnyCPU is
  the newer one, `/p:Platform=x86` was missed and the chain is running old code
- the `## ===== ... started =====` banner in today's log, for the restart
- `EncodersInUse` in the `status` Action, since an elevated rebuild wipes the ASCOM Profile
- a **zero-motion probe** of whatever changed. Feeding `slew-to-ha-dec` an out-of-range hour
  angle proved the new binary was live and found a second bug, without moving the telescope.
  Prefer one of these to assuming the build took.

## Taking the chain down and up

**The chain is the `Wise40Watcher` Windows service.** Stop it to take everything down, start it to bring everything back:

```powershell
Stop-Service  Wise40Watcher      # needs elevation
Start-Service Wise40Watcher
```

**Cycling the chain is pre-authorised** — Arie, 2026-09-18. Stop and start it as needed to take the chain down and up; no need to ask first.

It is `Automatic` start type. **Do not trust `CanStop` to tell you whether you may stop it** — it reported `True` from a non-elevated session on 2026-09-18, because it reflects whether the *service* accepts a stop (`SERVICE_ACCEPT_STOP`), not whether the *caller* has permission. The permission is in the service ACL, and `sc sdshow Wise40Watcher` says:

```
(A;;CCLCSWLOCRRC;;;IU)                 Interactive Users: query/enumerate/interrogate/read
(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)   Administrators:    the above plus RP (start), WP (stop)
```

Interactive Users have neither `RP` nor `WP`, so a non-elevated `Stop-Service` fails with access denied at the call, not at the capability check. Drive it through an elevated `Start-Process ... -Verb RunAs`, the same way as `regasm` and the ACP script copies — which raises a UAC prompt, so it is authorised but not unattended.

Do this **before** an elevated build — the RemoteServer holds `ASCOM.Wise40.Telescope.dll` open, and the build will not overwrite it while the chain runs.

**A pure constant change may not need an elevated rebuild at all.** COM registration only has to be refreshed when the interface or type library changes; editing a value in `movementParameters` changes neither. Build the x86 DLL (see below), then just restart the service — it will load the new binary from the registered codebase path.

## Close FocusMax before building — and check the build log, don't trust the timestamps

**FocusMax is an ASCOM client and holds `Common.dll`, `Hardware.dll`, `ASCOM.Wise40SafeToOperate.SafetyMonitor.dll` and `ASCOM.Wise40.TessW.ObservingConditions.dll` open.** ACP starts it (`assuring that FocusMax is running now...`) and it **outlives the observing run** — on 2026-09-17 it had been holding those DLLs since 20:14 the previous evening.

```
error MSB3027: Could not copy ... Exceeded retry count of 10.
Failed. The file is locked by: "FocusMax V6 (23228)"
```

It is **not part of the Wise40 chain**, so stopping the chain does not release it and a process scan for ASCOM/Wise40 processes will not find it. Before an elevated build: stop the chain **and close FocusMax**.

A failure in one project cascades confusingly — the Telescope project failing leaves Dash, ObservatoryMonitor and RemoteSafetyDashboard reporting `CS0006: Metadata file ASCOM.Wise40.Telescope.dll could not be found`, which looks like a missing-reference problem and is not.

**Verify a build actually produced a new binary.** Compare the DLL's timestamp against the source file's, or better, check a value you changed in the driver's own log output. A build that failed this way can leave every copy on disk — `obj/` included — at its previous version while nothing obviously complains. That cost a full telescope test on 2026-09-17: the move behaved identically because the running DLL was seven hours old.

## An elevated rebuild wipes ASCOM Profile values

Re-registering a driver for COM **deletes its profile subkey**, so the driver restarts on its *code default* and re-persists that. Anything held only in the profile is silently lost on every rebuild.

This cost real confusion on 2026-09-17: `EncodersInUse` was deliberately set to `New`, verified live, and came back `Old` after the next rebuild — no error, no message. The fix is to make the code default equal what the telescope should actually run on, not to rely on the persisted value. Applies to **every** profile-persisted setting, not just this one.

## The watcher's child processes

`Wise40Service` launches Dash and the others with `CreateProcessAsUser` and **neither `LoadUserProfile` nor `CreateEnvironmentBlock`** (`CreateProcessAsUserWrappercs.cs:64`). So a child has the user's token but not their profile: `Environment.GetFolderPath(MyDocuments)` comes back empty, `Path.Combine` then yields a **relative** path, and `File.Exists` reports a confident "no such file" for a file plainly there.

This is a **general defect** — every watcher-launched process has the same broken view of per-user paths and environment. It was worked around inside `MaxImFilterNames` only. Symptom to recognise: a path-dependent lookup that works in a normal shell and fails in Dash.

**Still unfixed.**

## Logs

`Debugger.LogDirectory()` rolls at **noon UT**, so a whole night lands in one directory — `C:\Wise40\Logs\<date>\`. Do not expect a midnight boundary.

`ASCOM.RemoteServer.txt` runs to **hundreds of MB**. Never read it; grep or awk it in a single pass.

## Startup, after the 2026-08-15 work

Was ~9½ minutes, now expected well under one. Two independent causes, both fixed: the unregistered TessW driver (3m44s), and 14 serialised `WeatherLogger` constructors at ~28s each (5m14s), caused by `weather` having its primary key on `(Time, Station)` so `WHERE station=? ORDER BY time DESC LIMIT 1` was a backward scan of 36.5M rows. An index `idx_station_time (Station, Time)` was added **live on the production database** and took every station under 200 ms.

Still there and still worth fixing:

- The watcher declares the RemoteServer healthy at **+1.6 s** using `Process.Responding`, which is true as soon as a message pump exists, then starts Alpaca and Dash into a server that will not answer for minutes.
- The reflection in `WiseDecEncoder.Angle:186` is commented out, so through the pole the live pointing path adds 12h to RA without reflecting Dec. Wants someone watching the mount cross the pole.

## Credentials in the repo

Raised repeatedly, unchanged: the **MySQL root password in plaintext** at `Common/Const.cs:209`, and a **GitHub PAT embedded in the `ASCOM.Wise40` remote URL**. Both in a repo on GitHub. The Arduino repo uses `credential.helper=wincred` (set 2026-08-15) because Git Credential Manager throws `HttpRequestException` before reaching the stored credentials.
