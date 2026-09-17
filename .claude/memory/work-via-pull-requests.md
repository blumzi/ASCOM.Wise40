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

**`gh` is NOT installed** (checked 2026-09-17), so a PR cannot be opened from the command line yet. Until it is, push the branch and hand over the compare URL:

    https://github.com/blumzi/ASCOM.Wise40/compare/master...<branch>?expand=1

Installing it (`winget install GitHub.cli`) would allow `gh pr create` directly. Note the remote URL has a PAT embedded in it, so `gh` will want its own auth rather than reusing that.

See [[notes-live-in-the-repo]] for where memories and plans go.
