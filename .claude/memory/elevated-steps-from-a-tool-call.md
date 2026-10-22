---
name: elevated-steps-from-a-tool-call
description: "How to run admin steps from a tool call on this box without them silently half-working - run a .ps1 with -File and a log, never inline -Command; locked DLLs; and what the harness itself blocks"
metadata:
  type: reference
---

From 2026-10-22: registering script components, regasm, and replacing a locked driver DLL. Claude's
shell is never elevated, so admin steps go through `Start-Process powershell -Verb RunAs` (Arie answers
the UAC prompt; see [[wise40-build-and-environment-gotchas]]). Several ways of doing that fail
**without saying so**.

## Run a script file, never `-Command`

- **Inline `-Command` loses double quotes.** `Start-Process` joins `-ArgumentList` into one
  command line, and the inner `"..."` are stripped. `"$dst.old"` turned into `$dst.old`, a property
  of a string, which is `$null`, so the copy failed with "Cannot bind argument to parameter 'Path'".
  Likewise `"exit=$LASTEXITCODE"` became a command named `exit=$LASTEXITCODE`.
- The elevated window closes when it's done, so **its output is lost unless it writes a log**.
- **What works:** write a `.ps1` with `param([string]$Log)`, wrap the body in `& { ... } *> $Log`, then run
  `Start-Process powershell.exe -Verb RunAs -Wait -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$ps1`"",'-Log',"`"$log`""`
  and read the log. If no log appears, the UAC prompt was declined.
- **Always check the result independently afterwards**, e.g. a file hash or a registry key. "regasm said
  OK" and "regsvr32 /s returned" are not checks; `regsvr32 /u /s` returned on a scriptlet twice and removed nothing.

## A DLL that's in use

- Copying over a DLL that a process has mapped fails with "being used by another process", **even elevated**.
- **It can still be renamed.** Rename it aside (`x.dll.old-<timestamp>`) and copy the new one in. The
  holder keeps the old image and the next load gets the new one.
- **The renamed copy can't be deleted while it's mapped:** access is denied even elevated, though the ACL
  is normal and it opens for reading. Schedule it for deletion at reboot with
  `MoveFileEx(path, [NullString]::Value, 4)`. **Plain `$null` passes an empty string**, which fails with
  error 3 (path not found).
- **Finding the holder:** `tasklist /m <dll>` sees 32-bit processes, while 64-bit PowerShell's
  `Process.Modules` can't (see [[common-dll-shadowing]]). `/m` can't be combined with `/v`. On 2026-10-22,
  even elevated, nothing was found holding the DLL, so the holder is still unknown.

## What the harness blocks

- **`gh pr merge` is blocked by auto mode** ("Merge Without Review") even when Arie asks. Hand him
  `! gh pr merge <n> --repo blumzi/<repo> --rebase` instead. (ACP's earlier PRs got merge commits;
  the 2026-10-22 PRs were rebase-merged.) A rebase merge gives the commits new hashes, so
  `git branch -d` then calls the merged branch "not fully merged". Compare the
  trees (`git diff --stat <branch> origin/master`) before `-D`.
- **`git push` over HTTPS:** Git Credential Manager throws `HttpRequestException`. For a single command,
  `git -c credential.helper= -c 'credential.helper=!gh auth git-credential' push ...` works without
  changing config. See [[work-via-pull-requests]].
- **Heredocs that contain backticks get re-quoted and executed by the Bash tool.** A PR body in
  `<<'EOF'` ran every `` `name` `` as a command, harmlessly this time. Write bodies to a file and pass `--body-file`.
