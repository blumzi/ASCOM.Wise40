# Elevated deploy: stop the chain, rebuild in dependency order, start it, then VERIFY.
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

# In dependency order.  BuildProjectReferences=false compiles against the DLLs already on
# disk, so a change in Hardware is invisible to Telescope until Hardware is rebuilt first.
# Telescope last: its bin\x86\Debug is what the COM registration points at and what the chain
# loads, and the project reference copies the fresh Hardware.dll in beside it.
# Dash after Telescope: it references Common, Hardware AND the Telescope driver, and with
# BuildProjectReferences=false it compiles against whatever DLLs are on disk at that moment.
# It is also a chain child (see $children), so it is already stopped before any of this and
# relaunched by the watcher afterwards - which is what puts the new GUI on screen.
#
# Wise40Service builds the WATCHER ITSELF - the service this script stops and starts.  That
# works only because the stop above has already happened by the time we build, so the exe is
# not locked; it would fail outright if the order were different.
#
# It was missing until 2026-09-21, which meant a fix to Watcher.cs could be committed, merged
# and "deployed" without the running service ever changing.  Note also that its output goes to
# bin\Debug rather than bin\x86\Debug despite the assembly being X86 - which is why it shows up
# in the sync below as an "AnyCPU-slot" file that is really x86.
#
$projects = @(
    "$repo\Common\Common.csproj",
    "$repo\Hardware\Hardware.csproj",
    "$repo\Telescope\Telescope.csproj",
    "$repo\Dash\Dash.csproj",
    "$repo\Wise40Service\Wise40Watcher.csproj"
)

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
Set-Content -Path $log -Value "=== elevated deploy ===" -Encoding utf8
function Say($m) {
    $line = "{0} (+{1,5:N1}s)  {2}" -f (Get-Date -Format 'HH:mm:ss'), ((Get-Date) - $t0).TotalSeconds, $m
    Add-Content -Path $log -Value $line -Encoding utf8
}
function LiveChildren { @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match $children }) }

Say ("elevated  : " + ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))

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
    Say "VERDICT: FAILED (chain would not stop; nothing was built, chain left as found)"
    exit 2
}
Say "STOP: all chain processes exited"

# ---- 2. BUILD in order, stop at the first failure ------------------------
$code = 0
foreach ($proj in $projects) {
    $name = Split-Path $proj -Leaf
    Say "BUILD: $name"
    & $msb $proj /p:Configuration=Debug /p:Platform=x86 /p:BuildProjectReferences=false `
           /t:Build /v:minimal /nologo /fl "/flp:logfile=$msblog;verbosity=normal;append=true"
    if ($LASTEXITCODE -ne 0) {
        Say ("BUILD: $name FAILED with exit code $LASTEXITCODE - stopping, not building against a half-built dependency")
        $code = $LASTEXITCODE
        break
    }
    Say ("BUILD: $name ok")
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
    Say "VERDICT: FAILED (build; inspect deploy-msbuild.log, then Start-Service Wise40Watcher)"
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

$syncPairs = @(
    @{ name = 'Common.dll';   src = "$repo\Common\bin\x86\Debug\Common.dll" },
    @{ name = 'Hardware.dll'; src = "$repo\Hardware\bin\x86\Debug\Hardware.dll" }
)
$syncFailed = $false
foreach ($pair in $syncPairs) {
    if (-not (Test-Path $pair.src)) { Say ("SYNC: source missing: " + $pair.src); $syncFailed = $true; continue }
    $src = Get-Item $pair.src
    $srcArch = AssemblyArch $src.FullName

    $targets = @()
    $targets += Get-ChildItem $repo -Recurse -Filter $pair.name -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\bin\\' -and $_.FullName -ne $src.FullName }
    $targets += Get-ChildItem "C:\Program Files (x86)\Common Files\ASCOM" -Recurse -Filter $pair.name -ErrorAction SilentlyContinue
    $targets += Get-ChildItem "C:\Program Files (x86)\ASCOM" -Recurse -Filter $pair.name -ErrorAction SilentlyContinue

    $n = 0; $skipped = @(); $stale = @()
    foreach ($t in ($targets | Sort-Object FullName -Unique)) {
        if ($t.LastWriteTime -ge $src.LastWriteTime) { continue }

        $arch = AssemblyArch $t.FullName
        if ($arch -ne $srcArch) {
            $skipped += ("{0} [{1}]" -f $t.FullName, $arch)
            continue
        }
        try {
            Copy-Item $src.FullName $t.FullName -Force -ErrorAction Stop
            $n++
            Say ("SYNC: updated " + $t.FullName)
        } catch {
            Say ("SYNC: FAILED on " + $t.FullName + " : " + $_.Exception.Message)
            $syncFailed = $true
        }
    }
    Say ("SYNC: {0}: source is {1}; {2} copy/copies updated" -f $pair.name, $srcArch, $n)

    #
    # Verify, RE-READING each file from disk.
    #
    # The $targets objects were captured by Get-ChildItem BEFORE the copies, so their
    #  LastWriteTime is the value from before the write.  Trusting it reported every file that
    #  had just been updated as still stale, failed the sync gate and left the chain down -
    #  2026-09-20, caught on the first run of this rule.
    #
    foreach ($t in ($targets | Sort-Object FullName -Unique)) {
        $now = Get-Item $t.FullName -ErrorAction SilentlyContinue
        if ($null -eq $now) { continue }
        if ($now.LastWriteTime -lt $src.LastWriteTime -and (AssemblyArch $now.FullName) -eq $srcArch) {
            $stale += $now.FullName
        }
    }
    if ($stale.Count -ne 0) {
        Say ("SYNC: STILL STALE after sync: " + ($stale -join '; '))
        $syncFailed = $true
    }

    # Different architecture: reported, deliberately not touched.
    if ($skipped.Count -ne 0) {
        Say ("SYNC: {0}: {1} copy/copies left alone, wrong architecture for this build:" -f $pair.name, $skipped.Count)
        foreach ($s in $skipped) { Say ("SYNC:   skipped " + $s) }
    }
}

if ($syncFailed) {
    Say "START: SKIPPED - assembly sync failed; starting now would run a mixture of builds."
    Say "START: THE CHAIN IS LEFT DOWN DELIBERATELY."
    Say "VERDICT: FAILED (sync)"
    exit 5
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
    Say "VERDICT: OK - chain up, correct binary answering, profile intact"
    exit 0
}
Say "VERDICT: DEPLOYED BUT NOT VERIFIED - see the VERIFY lines above"
exit 4
