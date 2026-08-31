'tabs=4
'------------------------------------------------------------------------------
'
' Script:       Wise40 Action Test.vbs
'
' Description:  Answers one question before a calibration night depends on it:
'               can an ACP script reach the Wise40 telescope driver's ASCOM
'               Action() method through ACP's Telescope object?
'
'               "Wise40 Calibration Run.vbs" records every calibration point by
'               calling
'
'                   Telescope.Action "calibration-point", "<ra>,<dec>"
'
'               If ACP's Telescope wrapper does not pass Action() through, that
'               run would complete a whole night and write nothing.  Better to
'               find out here, in ten seconds, than at dawn.
'
'               Nothing here moves the telescope, and nothing writes a
'               calibration point:  "status" is a pure read, and
'               calibration-point is called with an EMPTY parameter, which the
'               driver rejects with an error string before recording anything.
'               That still proves the whole path - name routed, driver reached,
'               answer returned.
'
'               As a bonus, the "status" digest reports the Renishaw readings
'               beside the encoders currently in use, so this doubles as a quick
'               look at how far apart the two are right now.
'
' Usage:        Run from the ACP scripting console.
'
'------------------------------------------------------------------------------
Option Explicit

'
' Only used if ACP's Telescope object turns out NOT to expose Action().  This is
' the ProgID of the telescope driver as ACP has it - adjust if the fallback
' reports that it cannot create the object.
'
Const DIRECT_PROGID = "ASCOM.AlpacaDynamic1.Telescope"

Sub Main()
    Dim actions, a, n, result, haveAction

    Console.PrintLine "=== Wise40 ASCOM Action test ==="
    Console.PrintLine ""

    '
    ' (1) What does the driver say it supports?
    '
    Console.PrintLine "(1) Telescope.SupportedActions"
    n = 0
    On Error Resume Next
    Set actions = Telescope.SupportedActions
    If Err.Number <> 0 Then
        Console.PrintLine "    **FAILED: " & Err.Description
        Console.PrintLine "      ACP's Telescope object does not expose SupportedActions."
        Err.Clear
        Set actions = Nothing
    End If
    On Error GoTo 0

    If Not actions Is Nothing Then
        For Each a In actions
            n = n + 1
            Console.PrintLine "    " & n & ". " & a
        Next
        If n = 0 Then
            Console.PrintLine "    (the driver advertises no actions at all)"
        End If
    End If
    Console.PrintLine ""

    '
    ' (2) A harmless read that returns something substantial.  "status" hands
    '     back the whole telescope digest as JSON and ignores its parameter
    '     entirely, so it changes nothing whatever we pass.  A long payload also
    '     exercises the round trip harder than a one-word answer would.
    '
    Console.PrintLine "(2) Telescope.Action ""status"", ""raw"""
    haveAction = False
    On Error Resume Next
    result = Telescope.Action("status", "raw")
    If Err.Number <> 0 Then
        Console.PrintLine "    **FAILED: " & Err.Description
        Err.Clear
    Else
        haveAction = True
        Console.PrintLine "    -> " & Len(result) & " characters of JSON"
        '
        ' The digest carries the Renishaw readings alongside the ones currently
        ' in use, so pull that fragment out - it is the interesting part.
        '
        Console.PrintLine "    Renishaw: " & Fragment(result, """Renishaw""")
        Console.PrintLine ""
        Console.PrintLine "    full digest:"
        Console.PrintLine "    " & result
    End If
    On Error GoTo 0
    Console.PrintLine ""

    '
    ' (3) The one that matters.  An empty parameter makes the driver answer with
    '     its usage error and record NOTHING, so this is safe to run any time.
    '     Seeing that error text back is the proof we came for.
    '
    Console.PrintLine "(3) Telescope.Action ""calibration-point"", """"   (expects a usage error, writes nothing)"
    On Error Resume Next
    result = Telescope.Action("calibration-point", "")
    If Err.Number <> 0 Then
        Console.PrintLine "    **FAILED: " & Err.Description
        Err.Clear
    Else
        Console.PrintLine "    -> " & result
        If InStr(result, "error:") = 1 Then
            Console.PrintLine "      Good - that is the driver's own reply, so the action is reachable."
        Else
            Console.PrintLine "      **Unexpected reply.  Expected a string starting ""error:""."
        End If
    End If
    On Error GoTo 0
    Console.PrintLine ""

    '
    ' (4) Only if ACP's wrapper let us down: does a direct ASCOM client work?
    '     This tells us the fallback is available BEFORE we need it.
    '
    If Not haveAction Then
        Console.PrintLine "(4) ACP's Telescope object did not pass Action() through."
        Console.PrintLine "    Trying a direct ASCOM client instead: " & DIRECT_PROGID
        TryDirect
        Console.PrintLine ""
    End If

    Console.PrintLine "=== done ==="
    If haveAction Then
        Console.PrintLine "Telescope.Action works.  ""Wise40 Calibration Run.vbs"" can record points."
    Else
        Console.PrintLine "Telescope.Action is NOT available through ACP's Telescope object."
        Console.PrintLine "The calibration run needs the direct-client approach - see (4) above."
    End If
End Sub

'------------------------------------------------------------------------------
' Pull one flat sub-object out of the digest JSON, for legibility.  Crude on
' purpose - it stops at the first closing brace, which is right only because
' RenishawDigest has no nested members.  Returns "(not found)" rather than
' failing, since this is decoration and must never break the test.
'------------------------------------------------------------------------------
Function Fragment(json, key)
    Dim i, j
    Fragment = "(not found)"
    i = InStr(json, key)
    If i = 0 Then Exit Function
    j = InStr(i, json, "}")
    If j = 0 Then Exit Function
    Fragment = Mid(json, i, j - i + 1)
End Function

'------------------------------------------------------------------------------
' The fallback: talk to the driver directly rather than through ACP.
'
' Note this attaches as an additional ASCOM client.  That is ordinary for a
' local server - ACP and the Dash are already connected - but it is why this is
' only attempted when the normal route has already failed.
'------------------------------------------------------------------------------
Sub TryDirect()
    Dim T, result

    On Error Resume Next
    Set T = CreateObject("ASCOM.DriverAccess.Telescope")
    If Err.Number <> 0 Then
        Console.PrintLine "    **Cannot create ASCOM.DriverAccess.Telescope: " & Err.Description
        Err.Clear
        Exit Sub
    End If

    T.DriverID = DIRECT_PROGID
    If Err.Number <> 0 Then
        Console.PrintLine "    **Cannot set DriverID to " & DIRECT_PROGID & ": " & Err.Description
        Console.PrintLine "      Edit DIRECT_PROGID at the top of this script."
        Err.Clear
        Exit Sub
    End If

    T.Connected = True
    If Err.Number <> 0 Then
        Console.PrintLine "    **Cannot connect: " & Err.Description
        Err.Clear
        Exit Sub
    End If

    result = T.Action("calibration-point", "")
    If Err.Number <> 0 Then
        Console.PrintLine "    **Action failed even directly: " & Err.Description
        Err.Clear
    Else
        Console.PrintLine "    -> " & result
        Console.PrintLine "      The direct client works.  The calibration script can use this."
    End If

    '
    ' Leave the driver as we found it.  Disconnecting our own client does not
    ' disturb ACP's or the Dash's - a local server counts its clients.
    '
    T.Connected = False
    Set T = Nothing
    On Error GoTo 0
End Sub
