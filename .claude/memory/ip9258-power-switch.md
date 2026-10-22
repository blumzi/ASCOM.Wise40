---
name: ip9258-power-switch
description: "The IP9258 power switch that feeds the Wise40 camera - its ASCOM driver, where its settings live, and the HTTPCMD_TRYLIMIT lockout that a wrong password causes and only a power cycle clears"
metadata:
  type: reference
---

Established 2026-10-22, on the hardware.

## What and where

- **Hardware:** Aviosys/Opengear **IP Power 9258**: 4 outlets per box. Box 0, first outlet
  (`P60`), is named `Camera` and feeds the STX16803. The other outlets are unused.
- **Driver:** `ASCOM.IPPower9258.Switch`, Arie's own, from `github.com/blumzi/PowerStrip`. It's
  cloned at `C:\Users\mizpe\source\repos\IP9258` (that local folder name is deliberate). Deployed to
  `C:\Program Files (x86)\Common Files\ASCOM\Switch\`, CLSID `{84FBB3F7-B195-451A-8782-5ECDF14745A8}`,
  registered in both the 32- and 64-bit views. The driver supports 4 boxes (16 outlets); only box 0 is enabled.
- **Settings are in the ASCOM Profile, not `Settings.json`:**
  `HKLM\SOFTWARE\WOW6432Node\ASCOM\Switch Drivers\ASCOM.IPPower9258.Switch\Box<n>` holds
  `IP Address`, `User`, `Password` (plaintext, and the tree is non-admin writable), `Enabled`, and
  `SwitchName0-3`/`SwitchDesc0-3`. Edit them through the driver's Setup dialog. See [[settings-live-in-json]]
  for the drivers that did move.
- **ACP side:** `Wise.Power` drives it on wise40. See the ACP repo's `wise40-power-ip9258` memory.

## `HTTPCMD_TRYLIMIT`: the lockout

The box answered `<html>HTTPCMD_TRYLIMIT</html>` (HTTP **200**) to every request for over half an
hour. **Cause: the password in the driver's profile was wrong.** Failed logins trip it, it does
not clear on its own, and **only a power cycle of the box cleared it**. After the power cycle, a wrong
password gets `401 Unauthorized` instead. It isn't in the manual, nor anywhere on the web or GitHub.

**How to apply:**
- If states look wrong, **send one request and stop**. Every failed retry feeds the lockout. Check the
  credentials with a single `GetPower` that sends `Authorization: Basic` up front. A correct one returns
  `<html>P60=0,P61=0,P62=0,P63=0,P64=0,P65=0,P66=0,P67=0</html>` (fields 5-8 are padding).
- Before 2026-10-22 the driver turned that error page into "outlet 0 is **ON**". It now throws instead.
  An old build that reports power on everything is lying.
- **A box power cycle applies the "Default outlet settings": all OFF here, so the camera loses power.**
  Turn it back on afterwards.
- **The Setup dialog's Enabled checkbox runs a live test, and a failed test saves the box disabled.** If you
  saved while the password was wrong or the box was locked, tick it again once it's fixed.

## Building and deploying the driver

- Build Release with `/p:PostBuildEvent= /p:TargetFrameworkVersion=v4.7.2`. The post-build step calls
  Inno Setup **5**, which isn't installed, and the 4.6.1 targeting pack isn't either.
- **Every deploy must re-run regasm.** `AssemblyVersion("6.0.*")` gives each build a new version, and a
  copied DLL that no longer matches the registered one fails to load with `0x80131040`.
  `tools\Deploy-IP9258.ps1` (in the PowerStrip repo) does the copy and both regasms, and elevates itself.
- The deployed DLL was locked by a process that nothing could identify. See [[elevated-steps-from-a-tool-call]].

## Manual

The Opengear *IP Power Quick Start* PDF (ftp.opengear.com) is the usable manual. It covers the HTTP command
format (pp. 20-22), outlet ids `P60`-`P63`, and password recovery (log in as `super user`, then power-cycle).
Passwords are at most 8 characters. Its SetPower reply example ends `</html?>`, which is a typo: the
real reply passes the driver's strict `</html>` check.
