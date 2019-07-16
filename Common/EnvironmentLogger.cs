using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using MySql.Data;
using MySql.Data.MySqlClient;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{
    public class EnvironmentLogger
    {
        private string _stationName;
        private StreamWriter _sw;
        Dictionary<string, string> _fileNames = new Dictionary<string, string>();
        private static MySqlConnection sql;
        private static Debugger debugger = Debugger.Instance;

        public EnvironmentLogger(string stationName)
        {
            _stationName = stationName;
            sql = new MySqlConnection("server=localhost;user=root;database=weather_events;port=3306;password=@!ab4131!@");
            try
            {
                sql.Open();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "EnvironmentLogger: sql.Open succeeded");
                #endregion
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "EnvironmentLogger: sql.Open failed: {0}", ex.StackTrace);
                #endregion
            }
        }

        static EnvironmentLogger() { }

        public void log(string sensorName, DateTime date, string value)
        {
            string currentFile = Debugger.LogDirectory() + "/Environment/" + sensorName + "/" + _stationName + ".dat";
            if (!_fileNames.ContainsKey(sensorName) || currentFile != _fileNames[sensorName])
            {
                Directory.CreateDirectory(Path.GetDirectoryName(currentFile));
                _fileNames[sensorName] = currentFile;
                if (_sw != null)
                    _sw.Close();
                _sw = new StreamWriter(_fileNames[sensorName], append: true);
            }

            _sw.WriteLine(date.ToString(@"HH:mm:ss.fff") + ',' + value);
            _sw.Flush();
        }
    }
}
