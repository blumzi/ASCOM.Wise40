using System;
using System.IO;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{

    public static class HumanIntervention
    {
        static DateTime _lastInfoRead = DateTime.MinValue;
        static string _info = null;

        static HumanIntervention() { }

        public static void Create(string oper, string reason)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Const.humanInterventionFilePath));
            using (StreamWriter sw = new StreamWriter(Const.humanInterventionFilePath))
            {
                sw.WriteLine("Operator: \"" + oper + "\"");
                sw.WriteLine("Reason: \"" + reason + "\"");
                sw.WriteLine("Created: " + DateTime.Now.ToString("MMM dd yyyy, hh:mm:ss tt") + " (local time)");
            }

            while (!File.Exists(Const.humanInterventionFilePath))
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        public static void Remove()
        {
            if (!File.Exists(Const.humanInterventionFilePath))
                return;

            bool deleted = false;
            while (!deleted)
            {
                try
                {
                    File.Delete(Const.humanInterventionFilePath);
                    deleted = true;
                }
                catch /*(System.IO.IOException ex) */
                {
                    ;
                }
            }
        }

        public static bool IsSet()
        {
            return System.IO.File.Exists(Const.humanInterventionFilePath);
        }

        public static string Info
        {
            get
            {
                string info = string.Empty;

                if (!IsSet())
                    return string.Empty;

                if (File.GetLastWriteTime(Const.humanInterventionFilePath) > _lastInfoRead)
                {

                    StreamReader sr = new StreamReader(Const.humanInterventionFilePath);
                    string line = string.Empty;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("Operator:") || line.StartsWith("Created:") || line.StartsWith("Reason:"))
                            info += line + "; ";
                    }

                    info = "Human Intervention; " + ((info == string.Empty) ? string.Format("File \"{0}\" exists.",
                        Const.humanInterventionFilePath) : info);
                    _info = info.TrimEnd(';', ' '); ;
                    _lastInfoRead = DateTime.Now;
                }
                return _info;
            }
        }
    }
}
