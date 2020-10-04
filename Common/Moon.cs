using System;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Astrometry;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public class Moon
    {
        private static AstroUtils astroutils = new AstroUtils();
        private static NOVAS31 novas31 = new NOVAS31();
        private static Object3 moon;
        private static CatEntry3 dummy_star;
        private static Observer observer;
        private static Debugger debugger = Debugger.Instance;

        // start Singleton
        private static readonly Lazy<Moon> lazy =
            new Lazy<Moon>(() => new Moon()); // Singleton

        public static Moon Instance
        {
            get
            {
                lazy.Value.init();
                return lazy.Value;
            }
        }

        private Moon() { }
        // end Singleton

        private void init()
        {
            double[] ScPos = { 0, 0, 0 };
            double[] ScVel = { 0, 0, 0 };
            InSpace ObsSpace = new InSpace();

            novas31.MakeInSpace(ScPos, ScVel, ref ObsSpace);
            novas31.MakeObserver(ObserverLocation.EarthSurface, WiseSite.Instance._onSurface, ObsSpace, ref observer);
            novas31.MakeCatEntry("DUMMY", "xxx", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, ref dummy_star);
            novas31.MakeObject(ObjectType.MajorPlanetSunOrMoon, 11, "Moon", dummy_star, ref moon);
        }

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
            return Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(long1 - long2));
        }

        public Angle Distance(double telescopeRA, double telescopeDec)
        {
            SkyPos moonPos = new SkyPos();
            short ret;

            ret = novas31.Place(astroutils.JulianDateUT1(0),
                moon,
                observer,
                0.0,
                CoordSys.Astrometric,
                Accuracy.Full,
                ref moonPos);

            if (ret != 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Moon.Distance: Cannot calculate Moon position (ret: {0})", ret);
                #endregion
                return Angle.FromDegrees(0);
            }

            double rad = SphereDist(telescopeRA, telescopeDec, moonPos.RA, moonPos.Dec);
            return Angle.AzFromRadians(rad);
        }
    }
}
