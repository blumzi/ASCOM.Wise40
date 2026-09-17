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

**`gh` is installed** at `%LOCALAPPDATA%\Programs\GitHub CLI\bin\gh.exe`, v2.101.0, and is on the user `Path`. So `gh pr create` works.

Installing it needed a detour worth remembering: `winget install --id GitHub.cli` fails with **`0x80070020` — "the process cannot access the file because it is being used by another process"**, both elevated and not. The MSI is the problem, not permissions. The portable **zip** from the same release installs cleanly with no elevation at all — download `gh_<ver>_windows_amd64.zip`, expand into `%LOCALAPPDATA%\Programs\GitHub CLI`, add its `bin` to the user `Path`. Use that route if it ever needs reinstalling or upgrading.

`gh` holds its **own** credential — deliberately not the PAT embedded in the git remote URL. `gh auth login` is interactive, so it has to be run by Arie, not from a tool call.

If `gh` is ever unavailable, the fallback is to push the branch and hand over:

    https://github.com/blumzi/ASCOM.Wise40/compare/master...<branch>?expand=1

See [[notes-live-in-the-repo]] for where memories and plans go.
