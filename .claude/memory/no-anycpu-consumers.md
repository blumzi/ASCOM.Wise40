---
name: no-anycpu-consumers
description: "Every assembly the Wise40 chain loads is x86 - a configuration NAMED Debug|AnyCPU can still set PlatformTarget x86, so read the PlatformTarget, never the configuration name"
metadata:
  type: reference
---

**Everything the chain loads is x86.** There are no AnyCPU consumers, despite what the solution
configuration names suggest.

## The trap

A configuration *named* `Debug|AnyCPU` can set `<PlatformTarget>x86</PlatformTarget>`. TessW and
RemoteSafetyDashboard do exactly that: they have no `Debug|x86` configuration at all, so the
solution maps them to `Debug|Any CPU`, and that config quietly targets x86.

On 2026-09-21 the solution's config mapping was read as if it were the platform:

| project | solution maps Debug|x86 to | actual PlatformTarget | built assembly |
|---|---|---|---|
| TessW | `Debug\|Any CPU` | **x86** | **X86** |
| RemoteSafetyDashboard | `Debug\|Any CPU` | **x86** | **X86** |
| Telescope, Common, Dash | `Debug\|x86` | x86 | X86 |

That produced a wrong conclusion - "TessW is an AnyCPU assembly loading an x86 Common, which only
works because the host is 32-bit" - and nearly a pointless, risky change to a working driver.

**Check the built artefact, not the project file and not the solution:**

```powershell
[Reflection.AssemblyName]::GetAssemblyName($path).ProcessorArchitecture
```

## Why TessW loads from bin\Debug

Because it has no x86 configuration, its output path is `bin\Debug` rather than `bin\x86\Debug`.
That is why TessW is the **one** driver whose COM `CodeBase` does not end in `bin\x86\Debug` -
see the live-path scan in `tools/deploy.ps1`, which reads the registry precisely so this exception
is handled rather than assumed away.

Moving it to `bin\x86\Debug` for uniformity was considered and **rejected**: it moves a registered
COM driver's `CodeBase`, needing an elevated re-registration, and buys nothing functional. The
`Common.dll` beside it is already x86 and correct.

## The MSIL Common is a leftover

`Common\bin\Debug\Common.dll` (MSIL) is **not** produced by the deploy. Under the `Debug|x86`
solution configuration `Common` builds x86 only, to `bin\x86\Debug`. Any MSIL copy is left over
from an older build and is reachable by nothing - which is why the deploy's stale-assembly gate
reports those copies as "abandoned output dir" rather than failing on them.

## What was actually inconsistent

`Release|AnyCPU` genuinely targeted AnyCPU in both projects, so a Release build would have
produced MSIL. Fixed 2026-09-21 to target x86. The deploy only ever builds Debug
(`/p:Configuration=Debug`), so it had never been exercised.

See [[common-dll-shadowing]] for why a wrong-architecture copy matters at all.
