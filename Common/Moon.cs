using System;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Astrometry;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public class Moon
    {
        public readonly SafeNovas31 novas31 = new SafeNovas31();
        public readonly SafeAstroutils astroutils = new SafeAstroutils();

        private static SkyPos moonPos = new SkyPos();
        private static Observer observer = new Observer();
        private static Object3 moonObject = new Object3();

        // start Singleton
        private static readonly Lazy<Moon> lazy =
            new Lazy<Moon>(() => new Moon()); // Singleton

        private readonly Exceptor MoonExceptor = new Exceptor(Debugger.DebugLevel.DebugMoon);

        public static Moon Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        private Moon() {}
        // end Singleton

        private void Init() { }

        public double Illumination
        {
            get
            {
                return astroutils.MoonIllumination(astroutils.JulianDateUT1(0));
            }
        }

        public double Phase
        {
            get
            {
                return astroutils.MoonPhase(astroutils.JulianDateUT1(0));
            }
        }

        //public short TopoPlanet(
        //    double JdTt,
        //    Object3 SsBody,
        //    double DeltaT,
        //    OnSurface Position,
        //    Accuracy Accuracy,
        //    ref double Ra,
        //    ref double Dec,
        //    ref double Dis
        //)

        ////8-----------------------------------------------------------------------------
        //function SphereDist(Long1:extended; Lat1:extended; Long2:extended; Lat2:extended): extended;
        ////------------------------------------------------------------------------------
        //// calculate the spherical distance between two coordinates
        //// Input : Long1 in radians [extended]
        ////         Lat1  in radians [extended]
        ////         Long2 in radians [extended]
        ////         Lat2  in radians [extended]
        //// Output: distance in radians [extended]
        //// EO - May 2002
        //        begin
        //   Result := arccos(sin(Lat1)*sin(Lat2) + cos(Lat1)*cos(Lat2)*cos(Long1 - Long2));
        //end;

        /// <summary>
        /// Angular distance between two directions.  ALL FOUR ARGUMENTS ARE RADIANS.
        /// </summary>
        public static double SphereDist(double long1, double lat1, double long2, double lat2)
        {
            return Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(long1 - long2)));
        }

        /// <summary>
        /// Where the Moon is now, as (RA radians, Dec radians).
        /// </summary>
        //
        // Kept separate from Distance so callers can have the position itself - the telescope
        //  driver exposes it as the "moon-position" Action - and so the unit conversion below
        //  exists in exactly one place.
        //
        // NOVAS reports SkyPos.RA in HOURS and SkyPos.Dec in DEGREES.  That cost a real bug:
        //  Distance() used to hand those straight to SphereDist, which takes radians, and so
        //  reported 19 degrees where the true separation was 77.  Always too small, always
        //  plausible - the worst kind of wrong for something meant to keep the telescope away
        //  from the Moon.
        //
        //
        // CACHED, AND NOT AT FULL ACCURACY.  Both matter more than they look.
        //
        // Every NOVAS call goes through SafeNovas31's cross-process mutex, because NOVAS is not
        //  thread-safe.  The Dash already computes the Moon on its refresh timer, and once the
        //  telescope driver exposed a moon-position action there were two processes competing for
        //  that mutex - at Accuracy.Full, which for the Moon is an expensive ephemeris.  On
        //  2026-09-24 that exceeded the 5-second timeout, the resulting DriverException killed the
        //  Dash while it held the mutex, and the chain could not recover.  SafeNovas31's abandoned
        //  -mutex handling is fixed now, but the right answer is not to generate the contention.
        //
        // The Moon moves about 0.5 deg/hour, so a 30-second cache is good to a quarter of an
        //  arcminute - far finer than anything this is used for - and it caps NOVAS traffic no
        //  matter how often callers ask.
        //
        // Reduced accuracy is milliarcseconds for the Moon, against a use case measured in
        //  degrees, and avoids the full JPL series.
        //
        private static DateTime _positionComputedAt = DateTime.MinValue;
        private static double _cachedRA, _cachedDec;
        private static readonly object _positionLock = new object();

        private static readonly TimeSpan PositionCacheLifetime = TimeSpan.FromSeconds(30);

        public void Position(out double raRadians, out double decRadians)
        {
            lock (_positionLock)
            {
                if (DateTime.UtcNow - _positionComputedAt < PositionCacheLifetime)
                {
                    raRadians = _cachedRA;
                    decRadians = _cachedDec;
                    return;
                }

                ComputePosition(out _cachedRA, out _cachedDec);
                _positionComputedAt = DateTime.UtcNow;
                raRadians = _cachedRA;
                decRadians = _cachedDec;
            }
        }

        private void ComputePosition(out double raRadians, out double decRadians)
        {
            WiseSite.InitOCH();
            novas31.MakeObserverOnSurface(WiseSite.Latitude, WiseSite.Longitude, WiseSite.Elevation,
                WiseSite.och.Temperature, WiseSite.och.Pressure, ref observer);
            novas31.MakeObject(ObjectType.MajorPlanetSunOrMoon, 11, "moon", new CatEntry3(), ref moonObject);

            short ret = novas31.Place(
                astroutils.JulianDateUT1(0),
                moonObject,
                observer,
                0.0,
                CoordSys.Astrometric,
                Accuracy.Reduced,   // milliarcseconds for the Moon; the full series is not needed here
                ref moonPos);

            if (ret != 0)
                MoonExceptor.Throw<InvalidOperationException>("Moon.Position", $"Cannot calculate Moon position (novas31.Place: {ret})");

            raRadians = Angle.Deg2Rad(Angle.Hours2Deg(moonPos.RA));   // hours -> degrees -> radians
            decRadians = Angle.Deg2Rad(moonPos.Dec);                  // degrees -> radians
        }

        /// <summary>
        /// Angular distance from the Moon.  Both arguments are RADIANS.
        /// </summary>
        public Angle Distance(double telescopeRA, double telescopeDec)
        {
            Position(out double moonRA, out double moonDec);
            return Angle.FromRadians(SphereDist(telescopeRA, telescopeDec, moonRA, moonDec));
        }
    }
}
