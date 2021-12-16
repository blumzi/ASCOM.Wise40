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

            using (StreamReader sr = new StreamReader("ha1.dat"))
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
                    if (fields.Length != 5)         // skip bad lines
                        continue;

                    int enc = Convert.ToInt32(fields[0]);
                    double ra = Convert.ToDouble(fields[1]);
                    double lst = Convert.ToDouble(fields[2]);
                    double wise40_ha = Convert.ToDouble(fields[3]);
                    double solved_ha = Convert.ToDouble(fields[4]);

                    double solved_hours = Angle.HaFromHours(util.ConditionHA(solved_ha)).Hours * -1;

                    Console.WriteLine(String.Format("enc: {0,10} wise-ha: {1,-20} solved-ha: {2,-20} delta: {3,-20}", enc, wise40_ha, solved_hours, Math.Abs(solved_hours - wise40_ha)));
                }
            }
        }
    }
}
