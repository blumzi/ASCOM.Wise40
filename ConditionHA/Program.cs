using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Wise40.Common;
using System.IO;

namespace ConditionHA
{
    class Program
    {
        static void Main(string[] args)
        {
            AstroUtils util = new AstroUtils();

            using (StreamReader sr = new StreamReader("ha.dat"))
            {
                string line;
                string[] fields;

                while ((line = sr.ReadLine()) != null)
                {
                    //Console.WriteLine($"line: {line}");
                    line = line.TrimStart().TrimEnd();
                    if (line.Length == 0)           // skip empty lines
                        continue;
                    if (line.StartsWith("#"))       // skip comments
                        continue;
                    fields = line.Split(' ');
                    if (fields.Length != 4)         // skip bad lines
                        continue;

                    double ra = Convert.ToDouble(fields[0]);
                    double lst = Convert.ToDouble(fields[1]);
                    double wise40_ha = Convert.ToDouble(fields[2]);
                    double solved_ha = Convert.ToDouble(fields[3]);

                    double solved_hours = Angle.HaFromHours(util.ConditionHA(solved_ha)).Hours * -1;

                    Console.WriteLine(String.Format("wise-ha: {0,-20} solved-ha: {1,-20} delta: {2,-20}", wise40_ha, solved_hours, Math.Abs(solved_hours - wise40_ha)));
                }
            }
        }
    }
}
