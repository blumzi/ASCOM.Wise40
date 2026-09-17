using System;
using System.Globalization;
using System.IO;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;        // RenishawHaEncoder/RenishawDecEncoder correction constants

namespace ASCOM.Wise40
{
    /// <summary>
    /// Records one calibration point per plate-solved sync, so that the Renishaw
    ///  encoders can be calibrated against the sky instead of against the old
    ///  encoders.
    ///
    /// Every ACP sync - FindLostScope.js and the pointing updates - lands in
    ///  WiseTele.SyncToCoordinates with the solved coordinates in hand.  At that
    ///  moment we read the local sidereal time and both raw Renishaw counts, in
    ///  the same breath, and that is the whole measurement:
    ///
    ///      HA(true) = LST - RA(solved)          paired with the HA count
    ///      Dec(true) = Dec(solved)              paired with the Dec count
    ///
    /// The pairing has to be SIMULTANEOUS - the count and the LST used with it -
    ///  rather than tied to mid-exposure.  While tracking, the telescope holds a
    ///  fixed position on sky, so the solved coordinates stay valid for as long as
    ///  it stays on that field; what moves is the axis, at one sidereal rate per
    ///  second, or 15 arcsec of hour angle. Read the count and the clock together
    ///  and the exposure's timing does not enter into it.
    ///
    /// What the points are FOR is worth stating, because it decides how to observe:
    ///
    ///  - The SLOPE of count against true angle gives the scale - the one number
    ///     we cannot get any other way, since the drums cannot be measured.
    ///
    ///  - The RESIDUALS about that line give the rest, and only a wide range of
    ///     angles can separate them.  A residual growing linearly is scale error
    ///     still unaccounted for.  A sinusoid, once per revolution, is eccentricity
    ///     of the drum: there is one read head per axis, so nothing cancels it, and
    ///     at the hour angle drum's ~111mm radius a mere 0.05mm of runout is some
    ///     93 arcsec.  A sinusoid in HA with a matching signature in Dec is polar
    ///     misalignment.
    ///
    /// So: spread the points over as much of each axis as it will reach.  Range
    ///  matters far more than the number of points - a line and a sine are only
    ///  confusable over a short arc.
    /// </summary>
    public static class RenishawCalibrationLog
    {
        private static readonly object _lock = new object();
        private const string FileName = "renishaw-calibration.csv";

        //
        // The two correction columns make the file self-describing, and they are the
        //  LAST columns on purpose.
        //
        // renishaw_ha_hours is a DERIVED value: it already has the zero-point
        //  correction applied.  Files written before 2026-09-17 have no correction in
        //  them at all, so that column is 0.192857h higher there than here for the same
        //  physical pointing.  Concatenate two nights from either side of that and fit
        //  the hours column, and the fit comes out wrong by exactly that offset while
        //  looking perfectly well behaved - the same failure mode as handing the driver
        //  J2000 coordinates instead of topocentric.
        //
        // Stamping the correction on every ROW rather than in a header line is what
        //  makes it survive concatenation, which is the whole point.  Appending rather
        //  than inserting keeps the first 14 columns where any existing reader expects
        //  them.
        //
        // Safest of all: fit on ha_count / dec_count.  Raw counts mean the same thing
        //  forever.
        //
        private const string Header =
            "utc,lst_hours,solved_ra_hours,solved_dec_deg,true_ha_hours," +
            "through_pole,axis_ha_hours,axis_dec_deg," +
            "ha_count,dec_count," +
            "old_ha_hours,old_dec_deg," +
            "renishaw_ha_hours,renishaw_dec_deg," +
            "ha_correction_hours,dec_correction_deg";

        /// <summary>
        /// The file for tonight.  Debugger.LogDirectory() rolls at noon UT, so a
        ///  whole night's points stay in one file instead of splitting at midnight.
        /// </summary>
        public static string Path =>
            System.IO.Path.Combine(Debugger.LogDirectory().Replace('/', '\\'), FileName);

        /// <summary>
        /// Records one point.  Never throws: a failure to log a calibration point
        ///  must not take a sync down with it.
        /// </summary>
        /// <param name="lstHours">local sidereal time, at the moment the counts were read</param>
        /// <param name="solvedRaHours">right ascension from the plate solve</param>
        /// <param name="solvedDecDegrees">declination from the plate solve</param>
        /// <param name="haCount">raw Renishaw hour angle count, as Position returns it</param>
        /// <param name="decCount">raw Renishaw declination count</param>
        /// <param name="oldHaHours">what the old encoder said, for comparison</param>
        /// <param name="oldDecDegrees">likewise</param>
        /// <param name="renishawHaHours">what the Renishaw says with today's constants</param>
        /// <param name="renishawDecDegrees">likewise</param>
        /// <param name="throughPole">
        /// whether the declination axis has gone past 90 degrees
        /// </param>
        public static void Record(
            double lstHours,
            double solvedRaHours,
            double solvedDecDegrees,
            bool throughPole,
            int haCount,
            int decCount,
            double oldHaHours,
            double oldDecDegrees,
            double renishawHaHours,
            double renishawDecDegrees)
        {
            try
            {
                //
                // Hour angle is LST - RA.  Said explicitly because getting this
                //  backwards is exactly how the 2024 calibration ended up with the
                //  Renishaw HA encoder reporting the negative of the hour angle.
                //
                double trueHaHours = lstHours - solvedRaHours;

                //
                // What the counts actually measure is the AXIS, and past the pole
                //  that is not where the telescope is looking:
                //
                //      axis Dec = 180 - Dec(sky)        axis HA = HA(sky) + 12h
                //
                // Recorded as its own pair of columns rather than left to whoever
                //  fits the data.  A point taken past the pole would otherwise sit
                //  in the file looking exactly like every other point, and land in
                //  the fit as a wild outlier - or worse, as a plausible one.  ACP's
                //  pointing mesh is generated in Alt/Az and excludes only the zenith
                //  and the horizon, so it will happily walk through the polar region.
                //
                double axisHaHours = trueHaHours;
                double axisDecDegrees = solvedDecDegrees;

                if (throughPole)
                {
                    axisDecDegrees = 180.0 - solvedDecDegrees;
                    axisHaHours = trueHaHours + 12.0;
                }

                while (axisHaHours > 12.0)
                    axisHaHours -= 24.0;
                while (axisHaHours < -12.0)
                    axisHaHours += 24.0;

                string path = Path;
                string directory = System.IO.Path.GetDirectoryName(path);

                lock (_lock)
                {
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    bool isNew = !File.Exists(path);

                    using (StreamWriter sw = new StreamWriter(path, append: true))
                    {
                        if (isNew)
                            sw.WriteLine(Header);

                        sw.WriteLine(string.Join(",", new string[]
                        {
                            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                            F(lstHours), F(solvedRaHours), F(solvedDecDegrees), F(trueHaHours),
                            throughPole ? "1" : "0", F(axisHaHours), F(axisDecDegrees),
                            haCount.ToString(CultureInfo.InvariantCulture),
                            decCount.ToString(CultureInfo.InvariantCulture),
                            F(oldHaHours), F(oldDecDegrees),
                            F(renishawHaHours), F(renishawDecDegrees),
                            F(RenishawHaEncoder.HaZeroPointCorrectionHours),
                            F(RenishawDecEncoder.DecZeroPointCorrectionDegrees),
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"RenishawCalibrationLog.Record: could not write {Path}: {ex.Message}");
                #endregion
            }
        }

        //
        // Enough digits to be worth having.  The hour angle counts turn over about
        //  every 0.12 arcsec, which is 2.2e-8 hours, so anything less than nine
        //  decimal places would throw away resolution we paid for.
        //
        private static string F(double d) => d.ToString("F9", CultureInfo.InvariantCulture);
    }
}
