---
name: notes-live-in-the-repo
description: "Arie wants Claude's memories and plans kept inside the ASCOM.Wise40 repo under .claude/, git-tracked, not in the per-user memory store"
metadata:
  type: feedback
---

Store memories in `<repo>\.claude\memory\` and plans in `<repo>\.claude\plans\`, both **git-tracked**. Create further subfolders under `.claude\` as suitable. Decided 2026-09-17.

**Why:** these notes are about this observatory and this code, so they should version with it, survive a machine rebuild, and be visible to anyone else who works on the telescope. A per-user store on one PC satisfies none of that.

**How to apply:** write new memories and plans into the repo, then commit them like any other change. The repo's `CLAUDE.md` carries the convention and a readable index of the memories — keep that index in step when adding or retiring one, since it is the entry point a new session actually sees.

Note the per-user store at `C:\Users\mizpe\.claude\projects\C--Users-mizpe\memory\` is what gets auto-loaded into context at session start, so its `MEMORY.md` is kept as a short pointer to this folder rather than as content. Don't let the two drift into rival copies — the repo is the original.
