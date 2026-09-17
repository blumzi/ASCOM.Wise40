'tabs=4
'------------------------------------------------------------------------------
'
' Script:       TestPointing.vbs
' Author:       Robert B. Denny <rdenny@dc3.com>
' Version:      6.0.1
' Requires:     ACP 5.1 or later!
'               Windows Script 5.6 or later
'
' Environment:  This script is written to run under the ACP scripting
'               console. 
'
' Description:  This script will check the pointing accuracy of ACP's corrector
'               and display the RMS pointing error when finished. It generates 
'               a random set of all-sky check-points, then walks theough them
'               using PinPoint to check the pointing error a each point.
'
' Revision History:
'
' Date      Who     Description
' --------- ---     --------------------------------------------------
' 04-Apr-03 rbd     Initial edit
' 19-May-03 rbd     Don't use UpdatePointing, avoid re-slew. More efficient.
' 01-Jul-03 rbd     Add Alt/Az output to script.
' 17-Jul-03 rbd     3.0.1 - Require 2 or more points.
' 31-Jul-03 rbd     3.1 - Re-version for minor release
' 07-Aug-03 rbd     3.1 - Fix CheckSlewLimits() for German mounts. Re-check
'                   slew limits just before using.
' 03-Mar-04 rbd     3.1.3 - Select Clear filter, use new focus offset call
' 04-Mar-04 rbd     3.1.3 - New SUP.Terminate() call at script end
' 18-Mar-04 rbd     3.2 - Now version 3.2
' 02-Aug-05 rbd     4.1.1 - Change to TakePointingImage for rotators, start
'                   with rotator at 0.
' 24-Aug-05 rbd     4.1.2 - Change to StartRotateToPA()
' 31-Oct-05 rbd     4.1.3 - SUP.HourAngle() gone, use Util.HourAngle12()
' 02-Sep-07 rbd     5.1.0 - Remove PP4 checks, have 5.0 now. Add TestPointing
'                   to log name. Account for possible offset slew in pointing
'                   update.
' 25-Oct-09 rbd     5.1.1 - (HF10) GEM:226 - Don't do local topo if scope 
'                   speaks J2000.
' 01-Nov-10 rbd     6.0.1 - (ACP6) GEM:471 Report alt/az and flip state.
'----------------------------------------------------------------------------
Option Explicit
' Variables used in multiple functions
Dim SUP                                                     ' Acquire support object
Dim FSO                                                     ' A FileSystemObject
Dim W40                                                     ' WISE40: our own ASCOM telescope client
Dim W40Failed                                               ' WISE40: True once we have given up

' WISE40: the telescope driver, as ACP has it configured.
'
' This MUST be the Alpaca dynamic client, which is registered LocalServer32 and
' therefore routes to the one running driver instance - the same one ACP is
' talking to.  Do NOT use "ASCOM.Wise40.Telescope": that is registered
' InprocServer32, so CreateObject would load the driver INTO ACP's own process
' and stand up a second WiseTele singleton contending for the same DAQ pins.
'
' Note this is created with a plain CreateObject.  "ASCOM.DriverAccess.Telescope"
' is not a registered ProgID - it is a .NET wrapper for .NET clients - and trying
' to CreateObject it fails with "ActiveX component can't create object".
Const WISE40_PROGID = "ASCOM.AlpacaDynamic1.Telescope"

'----------------------------------------------------------------------------------------
'
' ----------
' RecordCalibrationPoint() - WISE40: hand a solved point to the driver
' ----------
'
' ACP's Telescope object does not expose Action(), so we talk to the driver
' directly.  Coordinates must be local topocentric - see the caller.
'
' TWO THINGS THIS MUST NEVER DO, both because the Wise40 driver keeps a single
' shared instance rather than per-client state:
'
'   .Connected = ...    WiseTele.Connected is one flag on a singleton with no
'                       reference counting.  Setting it False here would run
'                       Connect(False) over every motor - the tracking motor
'                       included - both encoders and both axis monitors, pulling
'                       the mount out from under ACP mid-run.  We do not need it
'                       anyway:  Action() never checks Connected, and ACP has the
'                       telescope connected already or it could not be slewing.
'
'   .Dispose            Driver.Dispose() forwards to WiseTele.Dispose(), which
'                       disposes the singleton's disposables and clears its
'                       target coordinates.  Also shared, also still in use.
'
' So: create it once, keep it for the whole run, and let the script's teardown
' release it.
'
'----------------------------------------------------------------------------------------
Sub RecordCalibrationPoint(raHours, decDegrees)
    Dim reply

    If W40Failed Then Exit Sub                                  ' Complain once, not per point

    On Error Resume Next
    If Not IsObject(W40) Then
        Set W40 = CreateObject(WISE40_PROGID)
        If Err.Number <> 0 Then
            Console.PrintLine "  **Wise40: cannot create " & WISE40_PROGID & ": " & Err.Description
            Console.PrintLine "    No calibration points will be recorded this run."
            Err.Clear
            W40Failed = True
            On Error GoTo 0
            Exit Sub
        End If
    End If

    reply = W40.Action("calibration-point", InvNum(raHours) & "," & InvNum(decDegrees))
    If Err.Number <> 0 Then
        Console.PrintLine "  **Wise40 calibration-point failed: " & Err.Description
        Err.Clear
    ElseIf InStr(reply, "error:") = 1 Then
        '
        ' The driver answered, but refused the point.  Worth shouting about - the
        ' run would otherwise finish looking healthy with a short CSV.
        '
        Console.PrintLine "  **Wise40 calibration-point rejected: " & reply
    Else
        Console.PrintLine "  Wise40 calibration point recorded."
    End If
    On Error GoTo 0
End Sub

'----------------------------------------------------------------------------------------
'
' ----------
' InvNum() - WISE40: a number the driver can parse, whatever the locale
' ----------
'
' CStr() on a Double uses the machine's decimal separator.  Where that is a comma
' the result would both fail to parse as a Double on the C# side, which reads with
' InvariantCulture, and collide with the comma separating the two fields.
'
'----------------------------------------------------------------------------------------
Function InvNum(v)
    InvNum = Replace(CStr(v), ",", ".")
End Function

'----------------------------------------------------------------------------------------
'
' ----------
' CheckPoint() - Return the pointing error, arcmin, at coordinates
' ----------
'
' Parameters:
'   N                   Point number
'   RightAscension      J2000 RA of point, hours
'   Declination         J2000 Dec of point, degrees
'
' Returns:              Error in degrees, 0 means no data
'
'----------------------------------------------------------------------------------------
Function CheckPoint(N, RightAscension, Declination)
    Dim P, TgtName, ImageFile, RATrue, DecTrue, buf, CT
    Dim calRA, calDec, calOK                                    ' Wise40 calibration point
    '
    ' (1) Make target and file names (no extension). Delete old image file.
    '
    TgtName = "Map-" & CStr(N)
    ImageFile = Prefs.LocalUser.DefaultImageDir & "\" & TgtName ' No extension
    On Error Resume Next
    FSO.DeleteFile ImageFile & ".fts"                           ' Delete old image with this name
    On Error Goto 0
    '
    ' (2) Slew to the mapping point unless GEM and within 4 deg of meridian
    '
    Console.PrintLine "Doing point " & N & " - slew to point."
    If Telescope.AlignmentMode = 2 And Abs(Util.HourAngle12(RightAscension) * 15.0) < 4.0 Then
        Console.printLine "**Too close to meridian now. Cannot know flip state, skipped."
        CheckPoint = 0.0                                        ' Impossibly good value, error
        Exit Function
    End If
    SUP.StartSlewJ2000 TgtName, RightAscension, Declination
    If SUP.HaveRotator Then SUP.StartRotateToPA 0.0, RightAscension
    SUP.WaitForSlew
    If SUP.HaveRotator Then SUP.WaitForRotator
    '
    ' (3) Log this point for diagnosis
    '
    Set CT = Util.NewCTHereAndNow()
    CT.RightAscension = Telescope.RightAscension
    CT.Declination = Telescope.Declination
    Console.PrintLine "  Az = " & CInt(CT.Azimuth) & "  Alt = " & CInt(CT.Elevation)
    If Telescope.AlignmentMode = 2 Then
        If Util.GEMWestOfPier Then
            Console.PrintLine "  GEM looking west"
        Else
            Console.PrintLine "  GEM looking east"
        End If
    End If
    '
    ' (4) Take a pointing image. If successful, this leaves the scope at the
    ' possibly offset location.
    '
    If Not SUP.TakePointingImage(TgtName, ImageFile, RightAscension, Declination, 0.0) Then
        Console.PrintLine "**Failed to check at point " & N
        CheckPoint = 0.0                                        ' Impossibly good value, error
        Exit Function                                           ' Slewed back from offset
    End If
    '
    ' (5) Grab the true coordinates from the pointing image. We know it solved.
    '
    Set P = CreateObject("PinPoint.Plate")
    Call P.AttachNoImage(ImageFile & ".fts")                    ' Fast attach
    RATrue = P.RightAscension
    DecTrue = P.Declination
    '
    ' (5a) WISE40: hand this point to the telescope driver, which records it
    '      against the raw Renishaw encoder counts for calibration.
    '
    '      Note the coordinates MUST be topocentric.  PinPoint solves in J2000,
    '      the driver declares equTopocentric in ACP mode, and it forms the hour
    '      angle as LST - RA.  Handing over J2000 would be wrong by the accumulated
    '      precession - about 0.023h of RA, some 21 arcmin of hour angle - in every
    '      single point, and it would look perfectly consistent while being wrong.
    '
    '      Converted on COPIES, and unconditionally rather than under
    '      Prefs.DoLocalTopo, so that neither ACP's own error measurement below nor
    '      its configuration can affect what we record.
    '
    '      ACP's Telescope object does NOT pass Action() through - tested and
    '      confirmed - so the point goes via our own ASCOM client instead.  See
    '      RecordCalibrationPoint below.
    '
    calRA = RATrue
    calDec = DecTrue
    calOK = True
    On Error Resume Next
    SUP.J2000ToLocalTopocentric calRA, calDec
    If Err.Number <> 0 Then
        Console.PrintLine "  **Wise40: J2000ToLocalTopocentric failed: " & Err.Description
        Err.Clear
        calOK = False                                           ' LocalTopoRA/Dec would be stale
    End If
    On Error GoTo 0
    If calOK Then RecordCalibrationPoint SUP.LocalTopoRA, SUP.LocalTopoDec
    '
    ' (6) Return the pointing error
    '
    ' In case the pointing update required an offset slew, we need to grab
    ' the current scope coordinates and use them for error mesurement.
    ' do the measurement in local topo.
    '
    If Prefs.DoLocalTopo Then
        SUP.J2000ToLocalTopocentric RATrue, DecTrue             ' Convert true coords to local topo
        CheckPoint = SUP.EquDist(Telescope.RightAscension, Telescope.Declination, _
                            SUP.LocalTopoRA, SUP.LocalTopoDec)
    Else
        CheckPoint = SUP.EquDist(Telescope.RightAscension, Telescope.Declination, _
                            RATrue, DecTrue)
    End If
                                     
End Function

'----------------------------------------------------------------------------------------
'
' -----------------
' CheckSlewLimits() - Check alt/az coordinates for configured slew limits
' ----------------
'
' Cannot conveniently use Util.EnforceSlewLimits() because it is for the current
' time and takes RA/Dec. The mapping process takes time, so we must convert to 
' RA/Dec at the last possible moment. We need this check during winnowing of points
' before starting the mapping run... The conversion to RA/Dec for polar tilt-up 
' limit is not time sensitive.
'----------------------------------------------------------------------------------------
Function CheckSlewLimits(Az, Alt)
    Dim CT, RA, DE
    
    CheckSlewLimits = True                                  ' Assume success
    If Alt <= Prefs.MinimumElevation Then  
        CheckSlewLimits = False
        Exit Function
    End If
    If Alt <= Prefs.GetHorizon(Az) Then 
        CheckSlewLimits = False
        Exit Function
    End If
    Select Case Telescope.AlignmentMode
        Case 0:                                             ' ALt/Az mount
            If Alt >= Prefs.TiltUpLimit Then CheckSlewLimits = False
        Case 2:                                             ' German polar
            If Alt >= Prefs.TiltUpLimit Then CheckSlewLimits = False
        Case Else:                                          ' Simple polar (fork)
            Set CT = Util.NewCTHereAndNow()                 ' Need Dec for test
            CT.Azimuth = Az
            CT.Elevation = Alt
            If CT.Declination > Prefs.TiltUpLimit Then CheckSlewLimits = False
            Set CT = Nothing
    End Select
    
End Function

'----------------------------------------------------------------------------------------
'
' ------
' Main() - Main entry point for this script
' ------
'
'----------------------------------------------------------------------------------------
Sub Main
    Dim Az(), Alt(), V(), Cal()
    Dim CT, D, DE, DM, ET, F, I, J, M, N, NR, R, RA, SQE, buf, size   ' FORTRAN?
    Dim dtStart, dtEnd, CalFile, CalStream, MAX
    
    Set FSO = CreateObject("Scripting.FileSystemObject")
    
    dtStart = Util.SysUTCDate
    buf = "TestPointing-" & Util.FormatVar(dtStart, "dd-mmm-yyyy@HhNnSs") & ".log"    ' Typical Log Name
    Console.LogFile = Prefs.LocalUser.DefaultLogDir & "\" & buf     ' Log to this file
    Console.Logging = True

    Set SUP = CreateObject("ACP.AcquireSupport")
    Call SUP.Initialize()                                           ' (may log)
    If SUP.HaveFilters Then                                         ' If we have filters
        SUP.SelectFilter Prefs.CameraPrefs.ClearFilterNumber
    End If

    '
    ' (1) Input number of points
    '
    Call Console.ReadLine("Number of points (> 1, empty to stop)?", 3)
    buf = Console.LastReadResult
    If buf = "" Then 
        SUP.Terminate
        Exit Sub
    End If
    N = CInt(buf)
    If N < 2 Then
        Console.PrintLine "You must specify at least 2 points."
        SUP.Terminate
        Exit Sub
    End If
    Console.PrintLine "Beginning pointing check run at " & Util.FormatVar(dtStart, "Hh:Nn:Ss") & " UTC"
    If Prefs.PointingUpdates.Simulate Then Console.PrintLine "Simulating - exposure duration will be 10% of normal."
    '
    ' (2) Form new arrays of Alt/Az within configured slew limits 
    '     including not within 5 degrees of either pole.
    '
    ReDim Az(N)
    ReDim Alt(N)
    ReDim V(N)
    Randomize
    For I = 1 To N
        Do While True
           Az(I) = Rnd() * 360.0
           Alt(I) = ((80.0 - Prefs.MinimumElevation) * Rnd()) + Prefs.MinimumElevation
           If CheckSlewLimits(Az(I), Alt(I)) And (Abs(Alt(I)) < 85.0) Then
                V(I) = False                                   ' Not visited
                Exit Do
            End If
        Loop
    Next
    M = N
    '
    ' (3) Now map each point with lame attempt to minimize motion
    '
    ReDim Cal(M)                                                ' MaxPoint calibration records (strings)
    Set CT = Util.NewCTHereAndNow()
    N = 1                                                       ' Current point in array
    J = 0                                                       ' Simple valid mapped point counter
    Do While True
        Call CT.InitHereAndNow()                                ' Update transform
        CT.Azimuth = Az(N)
        CT.Elevation = Alt(N)
'       =======================================================
        If CheckSlewLimits(Az(N), Alt(N)) Then
            J = J + 1
            Cal(J) = CheckPoint(J, CT.RightAscension, CT.Declination)
            If Cal(J) = 0.0 Then J = J - 1                      ' Don't store failed mapping points
        Else
            Util.Console.PrintLine "(point skipped, out of slew limits now)"
        End If
'       =======================================================
        V(N) = True
        DM = 1000.0
        For I = 2 To M
            D = SUP.SphDist(Az(N), Alt(N), Az(I), Alt(I))
            If D < DM And Not V(I) Then
                DM = D
                NR = I
            End If
        Next
        If DM <>1000.0 Then 
            Console.PrintLine "   (move " & Util.FormatVar(DM, "0")  & " deg)"
        Else
            Exit Do
        End If
        N = NR
    Loop
    '
    ' (4) Report the RMS error
    '
    ' J is number of successful mapping points
    '
    SQE = 0.0
    For I = 1 to J
        SQE = SQE + ((Cal(I) * Cal(I)) / (J - 1))
    Next
    '
    ' (5) Wrap up...
    '
    dtEnd = Util.SysUTCDate
    ET = (CDbl(dtEnd) - CDbl(dtStart)) * 24.0
    Console.PrintLine "Pointing check run completed at " & Util.FormatVar(dtEnd, "Hh:Nn:Ss") & " UTC"
    Console.PrintLine "Elapsed time " & Util.Hours_HMS(ET)
    If J > 1 Then _
        Console.PrintLine "RMS pointing error is " & Util.FormatVar(Sqr(SQE) * 60.0, "0.000") & " arcmin."
    SUP.Terminate
    Console.Logging = False

End Sub                                                         ' MAIN
