using System;
using System.IO;

using ASCOM.Wise40.Common;
using Newtonsoft.Json;

namespace ASCOM.Wise40
{
    public static class HumanIntervention
    {
        static DateTime _lastInfoRead = DateTime.MinValue;
        //static string _info = null;
        static HumanInterventionDetails details;

        public class HumanInterventionDetails
        {
            public DateTime Created;
            public string Operator;
            public bool CampusGlobal;
            public string Reason;
        }

        static HumanIntervention() { }

        public static void Create(string oper, string reason, bool global = true)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Const.humanInterventionFilePath));
            using (StreamWriter sw = new StreamWriter(Const.humanInterventionFilePath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new HumanInterventionDetails()
                {
                    Operator = oper,
                    Reason = reason,
                    CampusGlobal = global,
                    Created = DateTime.Now,
                }, Formatting.Indented));
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

        public static HumanInterventionDetails Details
        {
            get
            {

                if (!IsSet())
                    return null;

                if (File.GetLastWriteTime(Const.humanInterventionFilePath) > _lastInfoRead)
                {
                    using (StreamReader file = File.OpenText(Const.humanInterventionFilePath))
                    {
                        JsonSerializer ser = new JsonSerializer();
                        details = (HumanInterventionDetails)ser.Deserialize(file, typeof(HumanInterventionDetails));
                    }
                    
                    _lastInfoRead = DateTime.Now;
                }
                return details;
            }
        }
    }
}
