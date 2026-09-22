---
name: settings-live-in-json
description: "Driver settings live in c:\\Wise40\\Settings.json, not the ASCOM Profile registry - registration still does. Reads never write, nothing seeds from the registry, and a section holds its own settings as leaves with sub-keys as nested objects"
metadata:
  type: project
---

**Driver settings are in `c:\Wise40\Settings.json`.** Not the ASCOM Profile, and no longer under
any `(root)` wrapper - both of those were true for a day and are not any more, so distrust older
notes and read `Common/WiseProfile.cs` if this disagrees with what you see.

## Why they moved

A build deleted them. `RegisterForComInterop` makes regasm run each driver's
`[ComUnregisterFunction]`, which calls `Profile.Unregister`, which deletes the **entire device
profile tree**. `ReadProfile` then re-created the settings from their **code defaults**, so
everything looked correct while holding whatever the source said. That is how a deliberate switch
to `EncodersInUse = New` was lost.

## The shape

A section holds its own settings directly; a sub-key is a nested object beside them.

```json
"SafeToOperate": {
    "AgeMaxSeconds": 180,
    "Bypassed": false,
    "Wind": { "Enabled": true, "Interval": 60, "Max": 40 }
}
```

Leaf-or-object is what tells a setting from a sub-key, so the lookups type-check: an object found
where a value was asked for reads as absent, and a write that would overwrite a whole sub-key is
refused and logged. No section currently has a setting sharing a name with a sub-key.

**Values are real JSON types**, round-trip guarded - a value only becomes a number if formatting
it back yields the original text, so RFIDs, IP addresses and `COM1` stay strings.

**SafeToOperate is keyed `[sensor][attribute]`**, not the reverse. The registry had it the other
way and the migration carried that over before it was corrected.

## Rules that are easy to get wrong

**A read never writes.** `GetValue` returns the caller's default when a value is absent and
persists nothing. It used to write the default back - a defence against the registry vanishing at
build time - which made the file a record of "something once asked for this" rather than of
decisions, and worse, bumped the file's write time and invalidated every other process's cache.

**Nothing seeds from the registry.** If the file is missing, every driver runs on code defaults
and `Load()` says so loudly. The registry is a frozen snapshot that no longer shares this file's
shape, and a stale seed is worse than none because it looks like data.

**Cross-process changes are picked up.** `Load()` compares the file's write time and re-reads.
This matters for the ordinary setup flow: an inproc driver's setup dialog runs in the **caller's**
process, not in the RemoteServer where the live instance sits.

**Writes are atomic.** `Save()` writes a temp file and `File.Replace`s it in. Readers deliberately
do not take the cross-process mutex, so a non-atomic copy let a reader see a half-written file,
fail to parse, and read every setting as its default until the next read.

**The lock is a Monitor, not a reader-writer lock** - reads serialise. Measured at ~24k reads in
12s against a concurrent writer, so it has not been worth changing.

See [[global-mutex-across-accounts]] for the DACL the cross-process mutex needs.

## The trap that cost the most

**A `Profile` field assigned far from its uses was missed by the migration.**
`WiseSafeToOperate._profile` was declared as `ASCOM.Utilities.Profile` and initialised in the
constructor; every sensor reads through it. The converter decided per `new Profile()` block by
looking ahead ~40 lines for value calls, saw none, and left it alone - so the whole safety system
stayed on the registry while everything else moved, silently, for a day.

It only surfaced when SafeToOperate was re-keyed: the code then asked the registry with the new
key order, every read missed, and the sun threshold quietly became the code default (-10 deg
rather than the configured 0).

**`ObservatoryMonitor.cs` has the same shape and is still on the registry** - not broken, because
its code was never re-keyed, but unmigrated. Check for `new Profile()` outside a registration
path before assuming a driver reads this file.

## Operating it

Back it up before a deploy, and keep taking `reg export "HKLM\SOFTWARE\WOW6432Node\ASCOM"` too -
that tree still holds **registration**, which is what puts a driver in the Chooser.

```
copy C:\Wise40\Settings.json C:\Wise40\Settings.json.predeploy
```

Windows is case-insensitive, so `Settings.json` and `settings.json` are the same file. An editor
left open on the old name can therefore write stale content straight over the live file.
