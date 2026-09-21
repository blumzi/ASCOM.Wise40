# Elevated deploy: stop the chain, rebuild the solution, start it, then VERIFY.
#
# Bundled into one script so there is a single UAC prompt.
#
# The point of the rewrite: "Start-Service returned" is not "the chain is up".  Wise40Watcher
# is a supervisor, so a Running service only means the WATCHER started - the endpoint took
# about 10 seconds longer than that on 2026-09-19.  The cycle now ends on functional checks.
#
# Every wait is bounded and every step is appended to the log as it happens, so interrupting
# this is safe: the log says exactly how far it got.

$ErrorActionPreference = 'Continue'

#
# PORTABLE PATHS.  This script used to hardcode a session scratch directory for its logs, which
# is precisely the kind of directory that gets cleaned up - the same way a previously installed
# gh.exe was lost.  A deploy script that cannot find its own log is not much of a deploy script.
#
# The repo is derived from where this file sits (<repo>\tools\deploy.ps1), so a clone anywhere
# works.  Logs go to TEMP, which always exists.  MSBuild can be overridden with WISE40_MSBUILD
# for a different Visual Studio edition.
#
$repo    = Split-Path -Parent $PSScriptRoot
$logDir  = Join-Path $env:TEMP 'wise40-deploy'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force $logDir | Out-Null }
$log     = Join-Path $logDir 'deploy.log'
$msblog  = Join-Path $logDir 'deploy-msbuild.log'
$base    = 'http://127.0.0.1:11111/api/v1/telescope/0'

$msb = $env:WISE40_MSBUILD
if (-not $msb) { $msb = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' }
if (-not (Test-Path $msb)) {
    Write-Host "MSBuild not found at $msb - set WISE40_MSBUILD to override." -ForegroundColor Red
    exit 6
}

# The solution, built whole - see the BUILD section for why a project subset cannot keep the
# ~30 Common.dll copies consistent.  MSBuild orders the projects from the reference graph, so
# there is no dependency list to maintain here and no BuildProjectReferences=false to get wrong.
#
# This also covers Wise40Service, which builds the WATCHER ITSELF.  That works only because the
# stop above has already happened by the time we build, so the exe is not locked.  It was absent
# from the old subset list until 2026-09-21, which meant a fix to Watcher.cs could be committed,
# merged and "deployed" without the running service ever changing.
#
$sln = "$repo\Wise40.sln"

#
# WHAT THIS LIST IS: every process that LOADS OUR ASSEMBLIES, and therefore has to be gone
# before we can rewrite them.  It is NOT "everything Wise40Watcher supervises" - those are two
# different sets and confusing them is dangerous in both directions.
#
# The watcher's set is Const.Apps in Common: RESTServer, Dash, SafetyDash, WeatherLink,
# ObservatoryMonitor, OCH and AlpacaClientLocalServer.  Which of them it actually starts depends
# on the operational mode - see Wise40Watcher.OnStart: ACP and WISE get the Dash, LCO also gets
# ObservatoryMonitor, SafetyDash is commented out, and WeatherLink only when the VantagePro
# driver is reading its HTML report (weatherLinkNeedsWatching).
#
# WEATHERLINK IS DELIBERATELY ABSENT.  It is Davis's own logger in c:\WeatherLink, it holds none
# of our DLLs - the VantagePro driver only reads a file it writes - and it is watched, running,
# and was never the thing blocking a build.  Adding it here would make the straggler kill below
# terminate the weather logger on every deploy, and weather is what SafeToOperate decides on.
# The only correct thing to do with WeatherLink during a deploy is leave it alone.
#
# ObservatoryMonitor and RemoteSafetyDashboard ARE here: both are built from this repo and carry
# their own Common.dll, so either one running would hold an assembly the sync step rewrites.
# Neither runs in ACP mode, which is why their absence has not bitten yet.
#
$children = '^(ASCOM\.RemoteServer|ASCOM\.OCH\.Server|ASCOM\.AlpacaClientLocalServer|Dash|ObservatoryMonitor|RemoteSafetyDashboard)$'
$x86 = "$repo\Telescope\bin\x86\Debug\ASCOM.Wise40.Telescope.dll"
$any = "$repo\Telescope\bin\Debug\ASCOM.Wise40.Telescope.dll"
$hw  = "$repo\Telescope\bin\x86\Debug\Hardware.dll"
$dash = "$repo\Dash\bin\x86\Debug\Dash.exe"

$t0 = Get-Date

#
# LOGGING MUST NOT BE ABLE TO SILENCE THIS SCRIPT.
#
# On 2026-09-21 the log stopped dead after "BUILD: Wise40.sln" and the deploy appeared to die
# without a word.  It had not: a `tail -f` had been opened on deploy.log to watch progress, which
# on Windows holds the file, so every later Add-Content failed.  Under
# $ErrorActionPreference = 'Continue' those failures were non-terminating and invisible, so the
# script ran to completion writing nothing - and the build-failure gate left the chain down with
# no way to say so.  Watching the log broke the log.
#
# So: every line goes to the CONSOLE first, which Start-Transcript captures to a second file, and
# the file write is best-effort with its own fallback.  No single locked file can hide a run.
#
function Say($m) {
    $line = "{0} (+{1,5:N1}s)  {2}" -f (Get-Date -Format 'HH:mm:ss'), ((Get-Date) - $t0).TotalSeconds, $m
    Write-Host $line
    try { Add-Content -Path $log -Value $line -Encoding utf8 -ErrorAction Stop }
    catch { try { Add-Content -Path "$log.alt" -Value $line -Encoding utf8 -ErrorAction Stop } catch {} }
}
try { Set-Content -Path $log -Value "=== elevated deploy ===" -Encoding utf8 -ErrorAction Stop }
catch { Write-Host "WARNING: $log is not writable ($($_.Exception.Message)) - using $log.alt and the console transcript" }
function LiveChildren { @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match $children }) }

#
# ALWAYS LEAVE A VERDICT.
#
# On 2026-09-21 this script logged "BUILD: Wise40.sln" and then nothing.  MSBuild finished 17
# seconds later with 7 errors, the process exited about five minutes after that, and no pass/fail
# line or VERDICT was ever written.  The chain was left down with no statement that it had been -
# which is the exact failure this script exists to prevent.  "Stopped and silent" is
# indistinguishable from "still working", and that ambiguity is what costs observing time.
#
# Two defences:
#   . Verdict() records that an outcome WAS stated.
#   . the exit handler fires on any path that did not state one - unhandled error, closed console,
#     kill - and both says so and PUTS THE CHAIN BACK.  A deliberate refusal has already written
#     its verdict and is left alone; only an UNPLANNED exit restarts the chain, because in that
#     case nobody chose to leave the observatory down.
#
$global:Wise40Verdict = $false
function Verdict($m) { $global:Wise40Verdict = $true; Say ("VERDICT: " + $m) }

# Restarting the chain and stating an outcome are separate jobs: the trap states its own, more
#  specific outcome and still needs the restart, so this must not be gated on the verdict flag.
function global:Wise40Recover($logPath) {
    function Note($m) { Add-Content -Path $logPath -Value ("{0}            {1}" -f (Get-Date -Format 'HH:mm:ss'), $m) -Encoding utf8 }
    try {
        if ((Get-Service Wise40Watcher -ErrorAction Stop).Status -ne 'Running') {
            Note "RECOVER: the chain is down and nothing chose that - starting Wise40Watcher"
            Start-Service Wise40Watcher -ErrorAction Stop
            Start-Sleep -Seconds 3
            Note ("RECOVER: service is " + (Get-Service Wise40Watcher).Status)
        }
    }
    catch { Note "RECOVER: FAILED - START Wise40Watcher BY HAND" }
}

function global:Wise40ExitNet($logPath) {
    if ($global:Wise40Verdict) { return }     # an outcome was stated; a refusal is deliberate
    Add-Content -Path $logPath -Encoding utf8 -Value `
        ("{0}            VERDICT: FAILED (terminated without reaching a verdict - see deploy-console.log)" -f (Get-Date -Format 'HH:mm:ss'))
    Wise40Recover -logPath $logPath
}
$null = Register-EngineEvent PowerShell.Exiting -Action ({ Wise40ExitNet -logPath $log }.GetNewClosure())

#
# A terminating error would otherwise unwind without a word.
#
# This must not assume Say exists.  A trap covers its whole scope regardless of where it appears,
# so it can fire on a statement that runs BEFORE the function definitions do - which is exactly
# what happened while testing this: the initial log write failed, the trap fired, and the trap
# itself then died on "The term 'Say' is not recognized", burying the original error.
#
trap {
    $m1 = "UNHANDLED: " + $_.Exception.Message
    $m2 = "UNHANDLED: line " + $_.InvocationInfo.ScriptLineNumber + ": " + $_.InvocationInfo.Line.Trim()
    Write-Host $m1; Write-Host $m2
    foreach ($m in @($m1, $m2, "VERDICT: FAILED (unhandled error)")) {
        try { Add-Content -Path $log -Value ("{0}            {1}" -f (Get-Date -Format 'HH:mm:ss'), $m) -Encoding utf8 -ErrorAction Stop }
        catch { try { Add-Content -Path "$log.alt" -Value $m -Encoding utf8 -ErrorAction Stop } catch {} }
    }
    $global:Wise40Verdict = $true
    if (Get-Command Wise40Recover -ErrorAction SilentlyContinue) { Wise40Recover -logPath $log }
    break
}

# The elevated window's console output died with the window on 2026-09-21, taking the only
#  record of what MSBuild said.  Keep a copy on disk.
try { Start-Transcript -Path (Join-Path $logDir 'deploy-console.log') -Force | Out-Null } catch {}

#
# ELEVATION IS A GATE, NOT A NOTE.  This used to be logged and ignored, and on 2026-09-21 that
# produced the worst possible outcome: Stop-Service failed with "Cannot open Wise40Watcher
# service", so the watcher stayed up and RELAUNCHED the children mid-sync; five assemblies were
# locked and silently left at the old build; and the script still started the chain and reported
# the children running.  A half-updated tree that reports success is worse than a refusal.
#
# Elevation is also required for its own sake: RegisterForComInterop makes the build run regasm,
# and a non-elevated build UNREGISTERS each driver and then fails to re-register it - which is
# exactly the DriverNotRegisteredException seen on 2026-08-15.
#
$elevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Say ("elevated  : " + $elevated)
if (-not $elevated) {
    Say "ABORT: not elevated.  Nothing has been stopped, built or copied."
    Say "ABORT: re-run this script from an Administrator shell."
    Verdict "FAILED (not elevated)"
    exit 4
}

# ---- 1. STOP, and treat failure to stop as fatal --------------------------
if ((Get-Service Wise40Watcher).Status -eq 'Stopped') { Say "STOP: already stopped" }
else {
    Say "STOP: stopping Wise40Watcher"
    try { Stop-Service Wise40Watcher -ErrorAction Stop } catch { Say ("STOP: FAILED: " + $_.Exception.Message) }
}
for ($i = 0; $i -lt 45; $i++) { if ((Get-Service Wise40Watcher).Status -eq 'Stopped') { break }; Start-Sleep -Seconds 1 }
Say ("STOP: service is " + (Get-Service Wise40Watcher).Status)

for ($i = 0; $i -lt 45; $i++) { if ((LiveChildren).Count -eq 0) { break }; Start-Sleep -Seconds 1 }
#
# STRAGGLERS GET KILLED, then we re-check.
#
# ASCOM.AlpacaClientLocalServer is a COM local server and does not always exit when the watcher
# stops - twice on 2026-09-20, each time leaving the whole chain down until someone killed it by
# hand.  The hard abort below is right about the danger (building against DLLs another process
# holds open can leave a partial mixture on disk) but wrong about the remedy: the service is
# already stopped, these processes are meant to be gone, and killing one that has outstayed its
# welcome is safer than leaving the telescope, dome and focuser offline.
#
# Still bounded, and the abort still stands if a kill does not take.
#
$live = LiveChildren
if ($live.Count -ne 0) {
    Say ("STOP: still running after the wait, killing: " + (($live | ForEach-Object { "$($_.ProcessName)($($_.Id))" }) -join ', '))
    foreach ($proc in $live) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction Stop; Say ("STOP: killed $($proc.ProcessName)($($proc.Id))") }
        catch { Say ("STOP: could not kill $($proc.ProcessName)($($proc.Id)): " + $_.Exception.Message) }
    }
    for ($i = 0; $i -lt 15; $i++) { if ((LiveChildren).Count -eq 0) { break }; Start-Sleep -Seconds 1 }
}

$live = LiveChildren
if ($live.Count -ne 0) {
    #
    # HARD ABORT.  This used to log a warning and build anyway, straight into the locked-DLL
    # failure: the RemoteServer holds ASCOM.Wise40.Telescope.dll open and MSBuild cannot
    # overwrite it, which can leave a partial mixture on disk.
    #
    Say ("STOP: ABORTING - still running: " + (($live | ForEach-Object { "$($_.ProcessName)($($_.Id))" }) -join ', '))
    Verdict "FAILED (chain would not stop; nothing was built, chain left as found)"
    exit 2
}
Say "STOP: all chain processes exited"

# ---- 2. BUILD THE SOLUTION ------------------------------------------------
#
# WHY THE WHOLE SOLUTION, NOT A LIST OF PROJECTS.
#
# Every driver references Common.csproj as a ProjectReference, and MSBuild's default for that is
# copy-local.  So a solution build propagates a fresh Common.dll into every consumer's bin as a
# BYPRODUCT of building them - which is how this repo kept ~30 copies consistent for years.
#
# Building a subset cannot do that, so the old script imitated copy-local with a file sync.  It
# could not work, for two structural reasons:
#
#   . It only ever held the x86 Common.  But the solution maps 17 projects to Debug|x86 and 11 to
#     Debug|Any CPU, so a real build produces TWO Commons - x86 and MSIL - and each consumer takes
#     the one matching its platform.  The sync correctly refuses to cross architectures (an x86
#     assembly cannot load into an AnyCPU host), so every MSIL consumer was simply never updated.
#   . It cannot overwrite a file held open by the running chain.
#
# Both bit on 2026-09-21: 23 copies updated, 5 locked, 8 skipped, and the two weather drivers ended
# up loading a Common that was not the one just built.
#
# Serial, NOT /m.  /t:Compile and /m together do not honour the solution's dependency ordering,
# and projects race ahead of Common - which produced 12 phantom "namespace Common does not exist"
# errors that vanished on a serial run.  The wall-clock saving is not worth diagnosing ghosts.
#
#
# Run MSBuild as a tracked process with a deadline, rather than "& $msb" inline.
#
# Inline, there is no way to tell "still compiling" from "this call will never return", and no
# upper bound on either - which is exactly the ambiguity that left the chain down and silent on
# 2026-09-21.  A tracked process gives a PID to report, a hard deadline, and output on disk
# instead of in a console window that dies with the window.
#
$code = 0
$buildOut = Join-Path $logDir 'deploy-msbuild-stdout.log'
$buildErr = Join-Path $logDir 'deploy-msbuild-stderr.log'
$deadline = 30      # minutes; a full 26-project solution build is minutes, not tens of minutes

Say "BUILD: Wise40.sln (Debug|x86, serial)"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $msb -PassThru -NoNewWindow `
        -RedirectStandardOutput $buildOut -RedirectStandardError $buildErr `
        -ArgumentList @(
            "`"$sln`"", '/p:Configuration=Debug', '/p:Platform=x86',
            '/t:Build', '/v:minimal', '/nologo', '/fl',
            "`"/flp:logfile=$msblog;verbosity=normal;append=true`""
        )
#
# TOUCHING .Handle IS LOAD-BEARING.  .NET only populates ExitCode and ExitTime if the process
#  HANDLE was retained, and Start-Process -PassThru without -Wait does not retain it.  Reading
#  .Handle here caches it, which is what makes the exit code readable after the wait.
#
# Without this the exit code comes back $null - and `$null -ne 0` is TRUE - so on 2026-09-21 a
#  CLEAN build (0 errors, 13.8s) was reported as "solution FAILED, exit code ," and the gate left
#  the chain down for nothing.  The giveaway was "after -63,925,595,215s", ExitTime unset.
#
$null = $proc.Handle
Say ("BUILD: msbuild pid " + $proc.Id + ", deadline " + $deadline + " min")

if (-not $proc.WaitForExit($deadline * 60 * 1000)) {
    Say ("BUILD: TIMED OUT after $deadline minutes - killing pid " + $proc.Id)
    try { $proc.Kill(); $proc.WaitForExit(30000) } catch { Say ("BUILD: kill failed: " + $_.Exception.Message) }
    $code = 9
}
else {
    $proc.WaitForExit()          # the timed overload can return before the handles settle
    $code = $proc.ExitCode
    if ($null -eq $code) {
        # Never let "I could not read the result" masquerade as "the result was failure" without
        #  saying so - that ambiguity is the whole bug above.
        Say "BUILD: exit code UNAVAILABLE despite a cached handle - treating as failure, but the build itself may have been fine; check deploy-msbuild.log"
        $code = 8
    }
    Say ("BUILD: msbuild exited {0} after {1:N0}s" -f $code, $sw.Elapsed.TotalSeconds)
}

if ($code -ne 0) {
    # Name the failures here.  Digging them out of a 1.2 MB MSBuild log is what turned a plain
    #  build failure into an afternoon on 2026-09-21.
    $errs = @(Select-String -Path $msblog -Pattern 'error [A-Z]+[0-9]+' -ErrorAction SilentlyContinue |
              ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    Say ("BUILD: solution FAILED, exit code $code, " + $errs.Count + " distinct error line(s)")
    foreach ($e in ($errs | Select-Object -First 10)) { Say ("BUILD:   " + $e.Substring(0, [Math]::Min(200, $e.Length))) }

    #
    # A non-zero exit with NOTHING to show for it means the exit code is more likely wrong than
    #  the build is.  Say that out loud rather than leaving a reader to conclude the tree is
    #  broken - on 2026-09-21 the log said "FAILED ... 0 distinct error line(s)" while MSBuild's
    #  own summary said "0 Error(s)", and the chain stayed down on the strength of it.
    #
    if ($errs.Count -eq 0) {
        $tail = @(Get-Content $msblog -Tail 15 -ErrorAction SilentlyContinue |
                  Where-Object { $_ -match 'Error\(s\)|Warning\(s\)|Build succeeded|Build FAILED|Time Elapsed' })
        Say "BUILD: NOTE - a failing exit code with no error lines is suspicious; MSBuild's own summary says:"
        foreach ($t in $tail) { Say ("BUILD:   " + $t.Trim()) }
    }
}
else {
    Say "BUILD: solution ok"
}

#
# NOT "newer than this script started".  That was the first attempt and it produced a false
# failure on 2026-09-19: all three projects exited 0 but MSBuild had skipped them as already
# up to date, because they had been built 8 minutes earlier, so nothing was rewritten and the
# gate left the chain down for no reason.  A build doing no work is a success.
#
# What actually matters:
#   . the DLL is not older than the newest source in its project  (genuinely up to date)
#   . the x86 DLL is newer than the AnyCPU one                    (catches a missed /p:Platform,
#     which exits 0 while writing bin\Debug and leaving bin\x86\Debug - what the chain loads -
#     untouched)
#
function NewestSource($dir) {
    $f = Get-ChildItem $dir -Recurse -Filter *.cs -ErrorAction SilentlyContinue |
         Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
         Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($f) { return $f.LastWriteTime }
    return [DateTime]::MinValue
}
$teleSrc  = NewestSource "$repo\Telescope"
$hwSrc    = NewestSource "$repo\Hardware"
$dashSrc  = NewestSource "$repo\Dash"
$x86ok = (Test-Path $x86) -and ((Get-Item $x86).LastWriteTime -ge $teleSrc)
$hwok  = (Test-Path $hw)  -and ((Get-Item $hw).LastWriteTime  -ge $hwSrc)
# Dash.exe, not a DLL: it is the process the watcher relaunches.  A stale exe here is the
# failure that would otherwise look like "the GUI change did nothing".
$dashok = (Test-Path $dash) -and ((Get-Item $dash).LastWriteTime -ge $dashSrc)
if (Test-Path $dash) { Say ("BUILD: Dash.exe       " + (Get-Item $dash).LastWriteTime.ToString('MM-dd HH:mm:ss') + "  newest source " + $dashSrc.ToString('MM-dd HH:mm:ss')) }
$platOk = (-not (Test-Path $any)) -or ((Get-Item $x86).LastWriteTime -gt (Get-Item $any).LastWriteTime)
if (Test-Path $x86) { Say ("BUILD: Telescope x86 " + (Get-Item $x86).LastWriteTime.ToString('MM-dd HH:mm:ss') + "  newest source " + $teleSrc.ToString('MM-dd HH:mm:ss')) }
if (Test-Path $hw)  { Say ("BUILD: Hardware.dll   " + (Get-Item $hw).LastWriteTime.ToString('MM-dd HH:mm:ss') + "  newest source " + $hwSrc.ToString('MM-dd HH:mm:ss')) }
if (Test-Path $any) { Say ("BUILD: AnyCPU         " + (Get-Item $any).LastWriteTime.ToString('MM-dd HH:mm:ss') + "  (must be older than x86)") }
Say ("CHECK: exit 0 [{0}]  Telescope up to date [{1}]  Hardware up to date [{2}]  Dash up to date [{3}]  x86 newer than AnyCPU [{4}]" -f ($code -eq 0), $x86ok, $hwok, $dashok, $platOk)

if (-not (($code -eq 0) -and $x86ok -and $hwok -and $dashok -and $platOk)) {
    Say "START: SKIPPED - build did not succeed cleanly."
    Say "START: THE CHAIN IS LEFT DOWN DELIBERATELY. Telescope, dome and focuser are offline."
    Verdict "FAILED (build; inspect deploy-msbuild.log, then Start-Service Wise40Watcher)"
    exit 3
}

# ---- 2c. SYNC every copy of Common.dll and Hardware.dll -----------------
#
# THE BUG THIS EXISTS FOR, 2026-09-20.  An hour-angle slew failed with
#   MissingMethodException: Angle.MechanicalDirection
# even though Common had been rebuilt, deployed and verified green.
#
# ASCOM.RemoteServer hosts SEVERAL Wise40 drivers - telescope, filter wheel, focuser, dome,
# SafeToOperate - and each is COM-registered with a codebase in its OWN bin folder, each of
# which carries its own copy of Common.dll.  Assembly identity is "Common, 1.0.0.0" with one
# public key token, so WHICHEVER DRIVER ACTIVATES FIRST supplies Common for the entire process.
# The filter wheel's copy was three days old, so the freshly built telescope bound to a Common
# that predated the method it had just been compiled against.
#
# That also means it is a RACE: it depends on activation order, which is why some earlier
# Common and Hardware changes appeared to work and others did not.
#
# MATCHED BY ARCHITECTURE, NOT BY FOLDER NAME.
#
# The first version of this synced anything under bin\x86\Debug and skipped bin\Debug, on the
# assumption that bin\Debug means AnyCPU.  It does not: bin\Debug holds whatever was last built
# into it, and twelve of those copies turned out to be X86 already - including the only two that
# are actually LOADED, by the watcher service and the OCH server.  Those were being skipped for
# a reason that was not true of them.
#
# The invariant is what makes this safe: this NEVER changes a file's architecture, it only
# replaces a file with a newer build OF THE SAME ARCHITECTURE.  An existing x86 copy is itself
# the evidence that whatever loads it is a 32-bit process, so replacing it in kind cannot break
# a host that was working.  Genuine MSIL copies are left alone and listed - an x86 assembly
# cannot load into a 64-bit process at all, so overwriting one could turn a working AnyCPU host
# into a BadImageFormatException.
#
function AssemblyArch($path) {
    try { return [string][System.Reflection.AssemblyName]::GetAssemblyName($path).ProcessorArchitecture }
    catch { return 'unreadable' }
}

#
# THIS NO LONGER COPIES ANYTHING.  The solution build's copy-local already put the right
# Common.dll and Hardware.dll beside every consumer, in the right architecture.  What is left to
# do is CHECK that claim, because a stale copy is invisible at runtime until it shadows the fresh
# one and something fails with a MissingMethodException - see .claude/memory/common-dll-shadowing.
#
# Checking both architectures is only possible now.  A subset build produced one Common; the
# solution produces two (x86 for the 17 x86 projects, MSIL for the 11 AnyCPU ones), so each copy
# can be compared against the build for ITS OWN architecture instead of being skipped.
#
# Scope of the gate: only projects the solution actually builds can be expected to be current.
# Abandoned trees (.sav folders, projects dropped from the solution) legitimately hold old copies
# and must not fail a deploy - they are reported and no more.
#
$slnDirs = @{}
foreach ($l in (Get-Content $sln)) {
    if ($l -match '^Project\("\{[0-9A-Fa-f-]+\}"\)\s*=\s*"[^"]+",\s*"([^"]+)"') {
        $rel = $matches[1]
        if ($rel -match '\.(csproj|vcxproj)$') {
            try {
                $full = [System.IO.Path]::GetFullPath((Join-Path $repo $rel))
                $slnDirs[[System.IO.Path]::GetDirectoryName($full).ToLower()] = $true
            } catch {}
        }
    }
}

$checkPairs = @(
    @{ name = 'Common.dll';   X86 = "$repo\Common\bin\x86\Debug\Common.dll";     MSIL = "$repo\Common\bin\Debug\Common.dll" },
    @{ name = 'Hardware.dll'; X86 = "$repo\Hardware\bin\x86\Debug\Hardware.dll"; MSIL = "$repo\Hardware\bin\Debug\Hardware.dll" }
)
$syncFailed = $false
foreach ($pair in $checkPairs) {
    $built = @{}
    foreach ($a in @('X86','MSIL')) {
        if (Test-Path $pair[$a]) { $built[$a] = (Get-Item $pair[$a]).LastWriteTime }
    }
    if ($built.Count -eq 0) {
        Say ("CHECK: {0}: NO BUILD OUTPUT - the solution did not produce it" -f $pair.name)
        $syncFailed = $true
        continue
    }
    foreach ($a in $built.Keys) { Say ("CHECK: {0} [{1}] built {2}" -f $pair.name, $a, $built[$a].ToString('MM-dd HH:mm:ss')) }

    $targets = @()
    $targets += Get-ChildItem $repo -Recurse -Filter $pair.name -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\bin\\' }
    $targets += Get-ChildItem "C:\Program Files (x86)\Common Files\ASCOM" -Recurse -Filter $pair.name -ErrorAction SilentlyContinue
    $targets += Get-ChildItem "C:\Program Files (x86)\ASCOM" -Recurse -Filter $pair.name -ErrorAction SilentlyContinue

    $shadow = @(); $orphan = @(); $current = 0
    foreach ($t in ($targets | Sort-Object FullName -Unique)) {
        $arch = AssemblyArch $t.FullName
        if (-not $built.ContainsKey($arch)) { $orphan += ("{0} [{1}]" -f $t.FullName, $arch); continue }

        # one second of slack: copy-local preserves the source stamp, but filesystem granularity
        #  and the copy itself can leave a sub-second difference.
        if ($t.LastWriteTime -lt $built[$arch].AddSeconds(-1)) {
            $projDir = ($t.FullName -split '\\bin\\')[0].ToLower()
            if ($slnDirs.ContainsKey($projDir)) { $shadow += ("{0} [{1}] {2}" -f $t.FullName, $arch, $t.LastWriteTime.ToString('MM-dd HH:mm')) }
            else { $orphan += ("{0} [{1}] not built by the solution" -f $t.FullName, $arch) }
        }
        else { $current++ }
    }

    Say ("CHECK: {0}: {1} copy/copies current, {2} stale in solution projects, {3} outside the solution" -f `
         $pair.name, $current, $shadow.Count, $orphan.Count)
    foreach ($o in $orphan) { Say ("CHECK:   ignored " + $o) }
    if ($shadow.Count -ne 0) {
        foreach ($s in $shadow) { Say ("CHECK:   STALE " + $s) }
        $syncFailed = $true
    }
}

if ($syncFailed) {
    Say "START: SKIPPED - a stale assembly survived the build; starting now would run a mixture."
    Say "START: THE CHAIN IS LEFT DOWN DELIBERATELY."
    Verdict "FAILED (stale assembly)"
    exit 5
}

# ---- 2c-bis. Report unguarded callbacks --------------------------------
#
# Reports, does NOT gate.  A grep is not an analyser, and refusing to deploy at 3am because a
# regex matched something new would be a worse failure than the thing it is guarding against.
# The count is in the log, where a rising number is visible to whoever reads it next.
#
try {
    $guardOut = & (Join-Path $PSScriptRoot 'check-guarded.ps1') 2>&1
    $fatalLine = ($guardOut | Where-Object { $_ -match 'KILL THE PROCESS' }) -join ''
    $silentLine = ($guardOut | Where-Object { $_ -match 'UNOBSERVED TASKS' }) -join ''
    Say ("GUARD: $fatalLine")
    Say ("GUARD: $silentLine")
} catch {
    Say ("GUARD: check-guarded.ps1 failed: " + $_.Exception.Message)
}

# ---- 2d. RECOVERY: make Windows restart the watcher if it dies ----------
#
# The watcher had NO failure actions configured, so when it crashed it simply stayed dead.  On
# 2026-09-21 it died at 03:59:15 and the four children ran orphaned until 10:35 - 6.6 hours,
# through the end of the night, with nothing supervising the observatory and nothing able to
# restart a Dash or a RemoteServer that failed.  Windows had recorded six such deaths.
#
# The crash itself is fixed in Watcher.cs, but a supervisor that can die and stay dead is worth
# a backstop regardless of the bug of the day.  Applied here, idempotently, so it survives a
# service reinstall rather than living in somebody's shell history.
#
# reset= 86400 : the failure count returns to zero after a quiet day
# actions=     : restart after 5s, then 10s, then 30s for subsequent failures
#
& sc.exe failure Wise40Watcher reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
if ($LASTEXITCODE -eq 0) { Say "RECOVERY: failure actions set (restart 5s/10s/30s, reset 24h)" }
else { Say "RECOVERY: sc failure returned $LASTEXITCODE - not fatal, continuing" }

# ---- 3. START -----------------------------------------------------------
Say "START: starting Wise40Watcher"
try { Start-Service Wise40Watcher -ErrorAction Stop } catch { Say ("START: FAILED: " + $_.Exception.Message) }
for ($i = 0; $i -lt 45; $i++) { if ((Get-Service Wise40Watcher).Status -eq 'Running') { break }; Start-Sleep -Seconds 1 }
Say ("START: service is " + (Get-Service Wise40Watcher).Status)

# ---- 4. VERIFY - this is where the cycle actually ends -------------------
# Children.  The watcher being Running says nothing about them.
$names = @()
for ($i = 0; $i -lt 60; $i++) {
    $names = (LiveChildren | ForEach-Object { $_.ProcessName } | Sort-Object -Unique)
    if ($names.Count -ge 4) { break }
    Start-Sleep -Seconds 1
}
Say ("VERIFY: children ({0}): {1}" -f $names.Count, ($names -join ', '))

# The endpoint.  First check that proves anything at all.
$connected = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $r = Invoke-RestMethod -Uri "$base/connected?ClientID=1&ClientTransactionID=1" -TimeoutSec 5
        if ($r.Value -eq $true) { $connected = $true; break }
    } catch { }
    Start-Sleep -Seconds 1
}
Say ("VERIFY: endpoint connected=$connected")

# EncodersInUse.  An elevated rebuild wipes the ASCOM Profile subkey; this is the known silent
# failure, and it has bitten before.
$encOk = $false; $enc = 'n/a'
if ($connected) {
    try {
        $st = Invoke-RestMethod -Method Put -Uri "$base/action" -TimeoutSec 10 `
                -Body @{ Action = 'status'; Parameters = ''; ClientID = 1; ClientTransactionID = 1 }
        if ($st.Value -match '"EncodersInUse":\s*(\d+)') { $enc = $Matches[1]; $encOk = ($enc -eq '1') }
    } catch { Say ("VERIFY: status action threw: " + $_.Exception.Message) }
}
Say ("VERIFY: EncodersInUse=$enc (1=New, expected) ok=$encOk")

# Zero-motion probe: proves the binary answering is the one just built, and cannot move the
# telescope.
#
# BACK to an out-of-range hour angle, and the range matters now.
#
# History: originally HourAngle=13, expecting "Invalid Hour Angle" from CheckCoordinateSanity.
# PR #40 disabled the Action outright, so the probe was changed to expect "disabled" instead.
# 2026-09-20 re-enabled it, which makes that probe ACTIVELY DANGEROUS: the version it replaced
# sent HourAngle=0,Declination=66 and would now command a real slew from a deploy script.
#
# 13 is outside the -12..+12 hour-angle range, so CheckCoordinateSanity throws before anything
# reaches a motor.  Never give this probe a value the mount could actually slew to.
$probeOk = $false; $probeMsg = 'not run'
if ($connected) {
    try {
        $pr = Invoke-RestMethod -Method Put -Uri "$base/action" -TimeoutSec 10 `
                -Body @{ Action = 'slew-to-ha-dec'; Parameters = 'HourAngle=13,Declination=66'; ClientID = 1; ClientTransactionID = 1 }
        $probeMsg = "$($pr.ErrorMessage)"
        $probeOk = ($probeMsg -match 'Invalid Hour Angle')
    } catch { $probeMsg = $_.Exception.Message }
}

# And that the newest Action is live, which the probe above cannot show.
$envOk = $false; $envMsg = 'not run'
if ($connected) {
    try {
        $er = Invoke-RestMethod -Method Put -Uri "$base/action" -TimeoutSec 10 `
                -Body @{ Action = 'test-envelope'; Parameters = ''; ClientID = 1; ClientTransactionID = 1 }
        $envMsg = "$($er.Value)$($er.ErrorMessage)"
        $envOk = ($envMsg -match 'test-envelope')
    } catch { $envMsg = $_.Exception.Message }
}
Say ("VERIFY: test-envelope ok=$envOk : $envMsg")
Say ("VERIFY: probe ok=$probeOk : " + ($probeMsg -replace '\s+', ' ').Substring(0, [Math]::Min(110, "$probeMsg".Length)))

# The lock-split in WisePin could only break things by making a readback fail.  Its own
# diagnostic says so, and the chain does thousands of pin operations coming up.
$today = (Get-Date).ToUniversalTime()
if ($today.Hour -lt 12) { $today = $today.AddDays(-1) }
$logdir = "C:\Wise40\Logs\{0:yyyy-MM-dd}" -f $today
$badReadback = 0
$rsLog = Join-Path $logdir 'ASCOM.RemoteServer.txt'
if (Test-Path $rsLog) {
    $badReadback = @(Select-String -Path $rsLog -Pattern 'did not read back' -ErrorAction SilentlyContinue).Count
}
Say ("VERIFY: 'did not read back' lines in today's log: $badReadback (0 expected)")

if ($connected -and ($names.Count -ge 4) -and $encOk -and $probeOk -and $envOk) {
    Verdict "OK - chain up, correct binary answering, profile intact"
    exit 0
}
Verdict "DEPLOYED BUT NOT VERIFIED - see the VERIFY lines above"
exit 4
