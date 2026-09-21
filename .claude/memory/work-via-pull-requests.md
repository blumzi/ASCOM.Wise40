---
name: work-via-pull-requests
description: "Arie wants changes to ASCOM.Wise40 to go through pull requests rather than commits straight to master, from 2026-09-17"
metadata:
  type: feedback
---

**Do not commit to `master` any more.** Branch, commit there, push the branch, open a PR. Decided 2026-09-17.

**Why it matters here specifically:** this repo drives a telescope. Several changes in recent memory were live-fire — an elevated rebuild and a chain restart put them straight onto the instrument the same night. A PR gives a place to see the diff and the reasoning before that happens, and a revert target if a night goes wrong.

**How to apply:**

- Branch from `master` with a short kebab-case name describing the change, matching what is already there (`axis-monitor`, `safety-and-shutdown`, `altaz-and-ha`).
- Keep the commit-per-concern habit — it survives into the PR and makes review readable.
- Push the branch, then open the PR.

**`gh` is installed** at `C:\Users\mizpe\bin\gh\bin\gh.exe`, v2.101.0, on the user `Path`. So `gh pr create` works.

**Call it by full path, and do not trust `command -v gh`.** A shell inherits its environment at
launch, so any session started before the `Path` edit — which is most of them, the edit having been
made once — reports `gh: command not found` while the binary sits exactly where this note says.
On 2026-09-21 that reading was taken at face value: the conclusion "gh not installed" was reported
to Arie and two PRs were handed over as `compare/...` URLs instead of being opened. **Check this
file before concluding a tool is missing** — the answer was already written down.

**Do not put it under a temp directory.** An earlier install went into a session scratchpad and was cleaned up, so a later session found no `gh` while this memory still claimed it was installed. `C:\Users\mizpe\bin\gh` is outside anything that gets swept.

**Why `winget install --id GitHub.cli` fails here, correctly diagnosed 2026-09-18.** It returns **`0x80070020` — "the process cannot access the file because it is being used by another process"**. This is *not* a property of the `gh` package, and not permissions. The machine has a **pending-reboot state**: `PendingFileRenameOperations` holds 104 entries including `C:\Config.Msi\*.rbf` (Windows Installer rollback files staged for deletion), and `CBS RebootPending` is set, on **42 days of uptime**. While `C:\Config.Msi` is in that state no new MSI transaction can get exclusive access, so **every MSI install on this box fails the same way** until it is restarted. Restarting takes the telescope chain down with it, so it belongs in a maintenance window.

The portable **zip** sidesteps the installer entirely and is the route to use meanwhile: download `gh_<ver>_windows_amd64.zip` from the release, expand into `C:\Users\mizpe\bin\gh`, add its `bin` to the user `Path` via `[Environment]::SetEnvironmentVariable('Path', ..., 'User')`. No elevation, no MSI, survives reboots.

A stale `C:\Users\mizpe\AppData\Local\Programs\GitHub CLI\bin` is still on the user `Path` from an earlier attempt and holds no `gh.exe`. Harmless, but if a `winget` install ever succeeds it will land there and shadow the zip copy.

`gh` holds its **own** credential — deliberately not the PAT embedded in the git remote URL. It is an OAuth token (`gho_`) in the **Windows keyring**, account `blumzi`, scopes `gist`/`read:org`/`repo`/`workflow`. The keyring is per-user and independent of the binary, so it **survived** the binary being deleted — reinstalling `gh` did not need `gh auth login` again. Only if `gh auth status` reports no account does that have to be run, and being interactive it has to be run by Arie, not from a tool call.

If `gh` is ever unavailable, the fallback is to push the branch and hand over:

    https://github.com/blumzi/ASCOM.Wise40/compare/master...<branch>?expand=1

See [[notes-live-in-the-repo]] for where memories and plans go.
