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

        public static double SphereDist(double long1, double lat1, double long2, double lat2)
        {
            return Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(long1 - long2)));
        }

        public Angle Distance(double telescopeRA, double telescopeDec)
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
                Accuracy.Full,
                ref moonPos);

            if (ret != 0)
                Exceptor.Throw<InvalidOperationException>("Moon.Distance", $"Cannot calculate Moon position (novas31.Place: {ret})");

            return Angle.FromRadians(SphereDist(telescopeRA, telescopeDec, moonPos.RA, moonPos.Dec));
        }
    }
}
