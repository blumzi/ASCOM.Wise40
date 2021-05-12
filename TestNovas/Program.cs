using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Astrometry;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace TestNovas
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Illumination: {Moon.Instance.Illumination}");
            try
            {
                Angle dist = Moon.Instance.Distance(WiseSite.Instance.LocalSiderealTime.Hours, Angle.DecFromDegrees(66.0).Degrees);

                Console.WriteLine($"Distance: {dist.ToNiceString()}");
            } catch (Exception ex)
            {
                Console.WriteLine($"Caught: {ex.Message} at {ex.StackTrace}");
            }
            System.Threading.Thread.Sleep(10000);
        }
    }
}
