# Flags callbacks that can take the process down, or lose their exception.
#
#   check-guarded.ps1            report and exit 0
#   check-guarded.ps1 -Strict    exit 1 if anything is unguarded
#
# WHY
#
# An exception escaping a callback thread terminates the process - not the callback, the process.
# This repo was bitten three times in two days: SafetyMonitorTimer.SafetyChecker took down the
# ASCOM server, Watcher.OnExit killed the service seven times (once leaving the observatory
# unsupervised for 6.6 hours), and the slewer tasks discarded a MissingMethodException that then
# cost a deploy cycle to find.
#
# Two hazards, opposite fixes:
#   . Timer / Thread / library-raised event -> KILLS the process.  Wrap with Guarded.
#   . Task.Run                              -> fails SILENTLY.     Observe with Guarded.Fire.
#
# This is a grep, not an analyser.  It will not catch every shape, and it is not meant to - it is
# meant to notice the next unguarded `new Timer(OnSomething)` before the telescope does.

param([switch]$Strict)

$repo = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem $repo -Recurse -Filter *.cs -ErrorAction SilentlyContinue |
         Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' -and $_.FullName -notmatch '\.sav\\' }

$fatal = @()   # kills the process
$silent = @()  # loses the exception

foreach ($f in $files) {
    $rel = $f.FullName.Substring($repo.Length + 1)

    # Guarded.cs IS the guard; of course it constructs tasks.
    if ($rel -eq 'Common\Guarded.cs') { continue }

    # A file that routes its event handlers through Guarded.Event has done the right thing;
    # the "+=" line itself then adds a variable and cannot be recognised by shape alone.
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $eventsAreGuarded = $text -match 'Guarded\.Event\('

    $n = 0
    $prevWasMarker = $false
    foreach ($line in [System.IO.File]::ReadAllLines($f.FullName)) {
        $n++
        $code = $line.Trim()

        #
        # The marker is checked FIRST, before the comment skip - it lives on a comment line of
        # its own, so skipping comments early meant $prev never saw it and the opt-out never
        # fired.  Explicit acknowledgement for sites guarded inline: PIDLibrary and
        # Wise40.Server deliberately do not reference Common and cannot use the helper.
        #
        $isMarker = $code -match 'guarded-inline'
        if ($prevWasMarker -and -not $isMarker) { $prevWasMarker = $false; continue }
        $prevWasMarker = $isMarker
        if ($isMarker) { continue }

        if ($code.StartsWith('//') -or $code.StartsWith('*')) { continue }
        if ($code -match 'Guarded\.') { continue }

        if ($eventsAreGuarded -and ($code -match '\.(Exited|Elapsed)\s*\+=')) { continue }

        if ($code -match 'new\s+(System\.Threading\.)?Timer\s*\(' -or
            $code -match 'new\s+Thread\s*\(' -or
            $code -match '\.Exited\s*\+=' -or
            $code -match '\.Elapsed\s*\+=') {
            $fatal += [pscustomobject]@{ File = $rel; Line = $n; Code = $code }
        }
        elseif ($code -match '\bTask\.Run\s*\(') {
            $silent += [pscustomobject]@{ File = $rel; Line = $n; Code = $code }
        }
    }
}

"UNGUARDED CALLBACKS THAT CAN KILL THE PROCESS: $($fatal.Count)"
$fatal | ForEach-Object { "   {0}:{1}  {2}" -f $_.File, $_.Line, $_.Code }
""
"UNOBSERVED TASKS (fail silently): $($silent.Count)"
$silent | ForEach-Object { "   {0}:{1}  {2}" -f $_.File, $_.Line, $_.Code }

if ($Strict -and $fatal.Count -ne 0) {
    ""
    "STRICT: $($fatal.Count) callback(s) can terminate their host. Wrap them with Guarded."
    exit 1
}
exit 0
