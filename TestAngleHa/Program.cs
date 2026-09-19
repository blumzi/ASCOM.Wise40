using System;
using ASCOM.Wise40.Common;

//
// Angle: radians <-> HMS conversions, and ShortestDistance over hour angles.
//
// Written 2026-09-19 after an HA-targeted Park slew reported 5.3502 rad for a 0.357 rad move and
// drove the primary axis about 80 degrees the wrong way into a limit switch.  Everything here is
// pure arithmetic - no telescope, no DAQ, no chain.
//
// Exit code is the number of failures, so this is usable as a check.
//
namespace TestAngleHa
{
    static class Program
    {
        const double HoursToRad = 2.0 * Math.PI / 24.0;
        const double Tolerance = 1e-6;

        static int failures;

        static void Fail(string what)
        {
            failures++;
            Console.WriteLine("    FAIL: " + what);
        }

        // ---------------------------------------------------------------------------------
        // 1. Representation.  This part PASSES, and is kept because it rules something out:
        //    negative hour angles round-trip exactly, so the defects below are not "negative
        //    numbers are mishandled", which was the first guess and was wrong.
        // ---------------------------------------------------------------------------------
        static void Representation()
        {
            Console.WriteLine("1. Angle.HaFromHours(v) round-trip");
            double[] vs = { -12, -7, -6.5, -3, -1.3624, -0.5, 0, 0.5, 1.3624, 3, 6.5, 7, 12 };
            foreach (double v in vs)
            {
                Angle a = Angle.HaFromHours(v);
                double wantRad = v * HoursToRad;
                if (Math.Abs(a.Hours - v) > Tolerance || Math.Abs(a.Radians - wantRad) > Tolerance)
                    Fail(string.Format("HaFromHours({0}): Hours={1} Radians={2}, wanted Hours={0} Radians={3}",
                                       v, a.Hours, a.Radians, wantRad));
            }
            Console.WriteLine("   done");
        }

        // ---------------------------------------------------------------------------------
        // 2. radians -> Angle for the HMS types.
        //
        //    FromRadians does `new Angle(rad * 180.0 / Math.PI, type)` - i.e. it converts to
        //    DEGREES whatever the type - and for an HMS type the constructor reads that number as
        //    HOURS.  Every such value is therefore 15x too large, 15 being degrees per hour.
        //    RaFromRadians and HaFromRadians repeat the same expression inline.
        //
        //    DecFromRadians and AzFromRadians are correct, because their types are degree-based.
        //    Rad2Hours already exists and is what these should use.
        // ---------------------------------------------------------------------------------
        static void RadiansToHms()
        {
            Console.WriteLine("2. radians -> Angle, HMS types (RA, HA)");
            double[] hours = { -6.5, -1.3624, 0.0, 1.3624, 6.5 };
            foreach (double h in hours)
            {
                double rad = h * HoursToRad;

                Angle ha = Angle.HaFromRadians(rad);
                if (Math.Abs(ha.Hours - h) > Tolerance)
                    Fail(string.Format("HaFromRadians({0:F6} rad): Hours={1:F6}, wanted {2:F6} (ratio {3:F3})",
                                       rad, ha.Hours, h, h == 0 ? 0 : ha.Hours / h));

                if (h >= 0)     // RA is 0..24, so only test the non-negative values
                {
                    Angle ra = Angle.RaFromRadians(rad);
                    if (Math.Abs(ra.Hours - h) > Tolerance)
                        Fail(string.Format("RaFromRadians({0:F6} rad): Hours={1:F6}, wanted {2:F6} (ratio {3:F3})",
                                           rad, ra.Hours, h, h == 0 ? 0 : ra.Hours / h));
                }
            }

            // The degree-based types, as a control: these should pass.
            foreach (double deg in new double[] { -35.0, 0.0, 66.0, 89.9 })
            {
                Angle d = Angle.DecFromRadians(deg * Math.PI / 180.0);
                if (Math.Abs(d.Degrees - deg) > Tolerance)
                    Fail(string.Format("DecFromRadians for {0} deg: Degrees={1}", deg, d.Degrees));
            }
            Console.WriteLine("   done");
        }

        // ---------------------------------------------------------------------------------
        // 3. ShortestDistance over hour angles.
        //
        //    HA is the ONLY AngleType that is both non-periodic and HMS, which is why this went
        //    unnoticed: Dec takes the same non-periodic branch but is degree-based, and RA and Az
        //    are periodic and take the other branch, which uses FromHours and never FromRadians.
        //
        //    Direction is expected to be correct in every case - it was, which is worth pinning
        //    down, because the axis still drove the wrong way.  That is a separate defect: the
        //    movementDict in WiseTele maps axisPrimary Increasing to EastMotor, which is right for
        //    RA and backwards for HA, where increasing hour angle means WEST.
        // ---------------------------------------------------------------------------------
        static void Check(double fromH, double toH, double wantHours, Const.AxisDirection wantDir)
        {
            ShortestDistanceResult r = Angle.HaFromHours(fromH).ShortestDistance(Angle.HaFromHours(toH));

            if (Math.Abs(r.angle.Hours - wantHours) > Tolerance)
                Fail(string.Format("distance {0,8:F4}h -> {1,8:F4}h: got {2,9:F4}h, wanted {3,8:F4}h (ratio {4:F3})",
                                   fromH, toH, r.angle.Hours, wantHours,
                                   wantHours == 0 ? 0 : r.angle.Hours / wantHours));

            if (r.direction != wantDir)
                Fail(string.Format("direction {0,8:F4}h -> {1,8:F4}h: got {2}, wanted {3}",
                                   fromH, toH, r.direction, wantDir));
        }

        static void Distances()
        {
            Console.WriteLine("3. ShortestDistance over hour angles");
            const Const.AxisDirection inc = Const.AxisDirection.Increasing;   // hour angle grows = WEST
            const Const.AxisDirection dec = Const.AxisDirection.Decreasing;   // hour angle shrinks = EAST

            // The move that actually happened: Park, from HA -01h21m44.8s to HA 0.
            Check(-1.3624, 0.0, 1.3624, inc);
            Check(0.0, -1.3624, 1.3624, dec);
            Check(1.3624, 0.0, 1.3624, dec);
            Check(0.0, 1.3624, 1.3624, inc);

            // Across the meridian, and out to the limits.
            Check(-3.0, 3.0, 6.0, inc);
            Check(3.0, -3.0, 6.0, dec);
            Check(-6.5, 0.0, 6.5, inc);
            Check(6.5, 0.0, 6.5, dec);
            Check(-6.5, 6.5, 13.0, inc);

            // No meridian crossing.
            Check(-5.0, -2.0, 3.0, inc);
            Check(-2.0, -5.0, 3.0, dec);
            Check(2.0, 5.0, 3.0, inc);

            // Degenerate.
            Check(0.0, 0.0, 0.0, Const.AxisDirection.None);
            Check(-1.3624, -1.3624, 0.0, Const.AxisDirection.None);
            Console.WriteLine("   done");
        }

        static int Main()
        {
            Console.WriteLine("Angle hour-angle tests\n");
            Representation();
            RadiansToHms();
            Distances();

            Console.WriteLine();
            if (failures == 0)
                Console.WriteLine("ALL PASS");
            else
                Console.WriteLine("{0} FAILURE(S) - see above", failures);
            return failures;
        }
    }
}
