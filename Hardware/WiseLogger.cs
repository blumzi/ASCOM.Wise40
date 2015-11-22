using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Wise40.Log
{
    public class WiseLogger
    {
        private string path;
        private StreamWriter sw;

        /// <summary>
        /// Looks for a directory in the upper hierarchy, if not found, uses "../logs".
        /// Opens a filestream to the append-only file "name".txt in the directory
        /// </summary>
        /// <param name="name"></param>
        public WiseLogger(string name)
        {
            string[] dirs = new string[] { "logs", "../logs", "../../logs", "../../../logs", "../../../../logs" };

            foreach (string dir in dirs)
            {
                if (Directory.Exists(dir)) {
                    path = dir + "/" + name + ".txt";
                    break;
                }
            }

            if (path == null) {
                Directory.CreateDirectory("../logs");
                path = "../logs/" + name + ".txt";
            }

            sw = (File.Exists(path)) ?  File.AppendText(path) : File.CreateText(path);
            sw.WriteLine("--------------------------------------------------------------------------------");
            sw.WriteLine("CWD:           " + Directory.GetCurrentDirectory());
            sw.WriteLine("Log-file path: " + path);
        }

        /// <summary>
        /// Writes a formatted line to the log-file.
        /// The arguments are prefixed with a time-stamp
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="o"></param>
        public void Log(string fmt, params object[] o)
        {
            DateTime now = DateTime.Now;
            sw.Write("{0}/{1}/{2}  {3}:{4}:{5}.{6} : ", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
            sw.WriteLine(fmt, o);
            sw.Flush();
        }
    }
}
