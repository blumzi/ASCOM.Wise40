---
name: diagnostics-that-break-what-they-measure
description: "Windows traps that make a healthy deploy look broken: tail -f locks a log and silences the writer, Start-Process -PassThru returns a null ExitCode, and enumerating all COM classes takes minutes"
metadata:
  type: reference
---

Three traps from 2026-09-21, all of which made a **working** deploy look broken and cost more
time than the real bugs did. They share a shape: the instrument interfered with the measurement.

## 1. `tail -f` on Windows silences the writer

Watching `deploy.log` with `tail -f` **holds the file**. Every subsequent `Add-Content` in the
script failed, and under `$ErrorActionPreference = 'Continue'` those failures were
non-terminating and invisible. The deploy ran to completion writing nothing after the point the
tail attached, so it looked like it had died mid-build. It had not - it hit a gate and left the
chain down deliberately, unable to say so.

**Do not `tail -f` a log a running tool is appending to on Windows.** Poll with `grep`, which
opens and closes, or read the file between calls.

`tools/deploy.ps1` now defends itself: every line goes to the console first (captured by
`Start-Transcript`), with the file write best-effort and a fallback to `deploy.log.alt`. No
single locked file can hide a run.

## 2. `Start-Process -PassThru` gives a null ExitCode

.NET populates `ExitCode` and `ExitTime` only if the process **handle** was retained, and
`Start-Process -PassThru` *without* `-Wait` does not retain it. So a clean build reported:

```
BUILD: msbuild exited  after -63,925,595,215s
BUILD: solution FAILED, exit code , 0 distinct error line(s)
```

`$null -ne 0` is **true**, so "I could not read the result" silently became "the result was
failure", and the gate left the chain down on a build that had 0 errors in 13.8s. The absurd
duration is the tell - `ExitTime` was never set.

Fix: read `$proc.Handle` immediately after `Start-Process` to cache it. Verified directly -
with a process exiting 3, `ExitCode` is `$null` without the touch and `3` with it.

**Corollary now built into the deploy:** a non-zero exit with **zero** error lines means the exit
code is more likely wrong than the tree is, so it prints MSBuild's own summary rather than
asserting failure.

## 3. Enumerating `Classes\CLSID` takes minutes

Walking every key under `HKLM\SOFTWARE\WOW6432Node\Classes\CLSID` - about **2954**, two registry
opens each, through the PowerShell registry provider - to find ~10 Wise driver paths took a
deploy from "built in 6 seconds" to crawling for two minutes and then dying outright.

Reverse the direction: read the ~14 Wise ProgIDs from the ASCOM Profile's driver lists, then
`ProgID -> CLSID -> InprocServer32` for each. **0.25s**, identical results.

Mind the two registry views, because mixing them up returns **nothing at all** rather than
failing loudly: ProgID keys are NOT WOW-redirected (`SOFTWARE\Classes\<ProgID>`, 64-bit view)
while the CLSID keys they point at ARE (32-bit view). A first attempt looked for ProgIDs under
`WOW6432Node`, ran in 0.84s and reported **zero** live paths - a gate that is silently useless
rather than visibly slow.

## 4. A watcher that matches its own subject

A loop waited for the deploy to finish with `grep -q 'VERDICT' deploy.log`. It returned on the
first poll every time, reporting a stale snapshot that made green runs look stalled - because
`deploy.ps1` prints this the moment the chain goes down:

```
STOP: >>> ... If this run dies without a VERDICT line,
```

The word the watcher was waiting for is in the safety hint. Three separate "the deploy seems
hung" diagnoses came from that before the cause was found. Anchor on the real line - `VERDICT:`
with the colon - and be suspicious when a wait returns instantly.

## The general lesson

When a long-running tool goes quiet, **suspect the observation before the tool**. In all three
cases the deploy was behaving correctly and the diagnostic was lying - twice by being silent,
once by being slow enough to look hung. Check the artefact the tool writes, not the pipe you are
watching it through.
