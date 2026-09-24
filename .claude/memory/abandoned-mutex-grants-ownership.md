---
name: abandoned-mutex-grants-ownership
description: AbandonedMutexException means the wait SUCCEEDED and you own the mutex; throwing from that catch holds it forever and no restart clears it
metadata:
  type: project
---

`Mutex.WaitOne` throwing `AbandonedMutexException` does **not** mean the wait failed. It means
the previous owner died without releasing, and **the waiter now owns the mutex**. It is a
warning delivered instead of a `true` return value, not an error.

Every `Safe*` wrapper in `Common` got this backwards. Thirteen sites — seven in
`SafeNovas31.cs`, five in `SafeAstroutils.cs`, one in `SafeAscomutil.cs` — did:

```csharp
catch (AbandonedMutexException ex)
{
    throw new DriverException(...);   // never releases what it was just granted
}
```

So the mutex stayed held forever. Worse, it is self-perpetuating across processes: the next
process to wait gets the same exception from the same dead ownership, throws, and adds its own
abandonment. On 2026-09-24 this took the whole chain down — the telescope included — and
**killing individual processes did not clear it**, because each replacement walked straight
back into the same path. Only a full service stop (`deploy.ps1`'s stop phase) ended it.

**Why:** a cross-process mutex has no owner to blame once its holder is gone; the OS hands the
lock to the next waiter precisely so the system can recover. Refusing that hand-off converts a
recoverable death into a permanent deadlock.

**How to apply:** in any `catch (AbandonedMutexException)`, set the got-it flag and fall
through to the work — the existing `finally` releases. If the protected state could have been
left inconsistent by the dead owner, *that* is worth logging or repairing, but never by
throwing without releasing. Check any new wrapper added alongside these three.

The trigger that exposed it is worth remembering too, and is its own lesson: a `moon-position`
driver Action called NOVAS at `Accuracy.Full` on every request, through this same mutex, while
the Dash computed the Moon on its refresh timer. Two processes contending on an expensive
ephemeris exceeded the 5-second wait. See [[diagnostics-that-break-what-they-measure]] —
a read-only-looking feature can still be the thing that breaks the observatory. `Moon.Position`
now caches for 30 s and uses `Accuracy.Reduced`.

Related: [[global-mutex-across-accounts]] for the other way these mutexes bite.
