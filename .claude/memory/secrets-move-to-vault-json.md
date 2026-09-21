---
name: secrets-move-to-vault-json
description: "Planned 2026-09-21: move the MySQL root password and the GitHub PAT into c:\\Wise40\\vault.json, outside git - and note that relocating is not rotating, since the password is already public in this repo's history"
metadata:
  type: project
---

**Decision, 2026-09-21 (Arie): the MySQL root password and the GitHub PAT move into
`c:\Wise40\vault.json`, outside git.** Not yet done.

`c:\Wise40\` is the right home: already outside the repo, already holds `settings.json`, already
readable by both LocalSystem and the logged-in user, and already part of the backup habit. See
[[settings-live-in-json]].

## Where they are today

| secret | where | in git history? |
|---|---|---|
| MySQL root password | plaintext at `Common/Const.cs:209` | **YES** - committed, and this repo is public on GitHub |
| GitHub PAT | embedded in the `origin` remote URL, in `.git/config` | No - `.git/config` is not committed |

## Four things to get right, in order of importance

**1. Relocating is not rotating.** The MySQL password is in the history of a **public** repository.
Deleting it from `Const.cs` does not un-publish it; anyone who cloned the repo already has it.
**It must be rotated**, and the vault only protects the new value. This is the whole reason the
item is urgent rather than tidy-up.

**2. The PAT probably should not go in the vault at all.** It is in the remote URL, which is why
every push has needed
`sed -E 's/ghp_[A-Za-z0-9]+/ghp_***REDACTED***/g'` on its output. The cleaner fix is to take it
out of the URL entirely and let Windows Credential Manager hold it - `gh` already keeps its own
separate OAuth token (`gho_`) in the Windows keyring, which survived `gh` being deleted and
reinstalled. A vault file would just be a third place a credential lives.

**3. `c:\Wise40` is non-admin writable.** That is deliberate and load-bearing - it is how drivers
save settings without elevation - but it means a secrets file there is readable by **any local
user**. ACL `vault.json` explicitly rather than inheriting the directory's permissions.

**4. Keep it out of the settings plumbing.** `WiseProfile` self-seeds `settings.json` from the
registry and writes defaults back on read; `vault.json` must be a separate reader with no
seeding, no defaults-write-back, and nothing that can reach `Debugger.WriteLine` with a value in
it. A secret that lands in a 2 GiB nightly log is not a secret.

## Also worth doing at the same time

`git log -S` the history for other plaintext credentials before assuming these two are the only
ones.
