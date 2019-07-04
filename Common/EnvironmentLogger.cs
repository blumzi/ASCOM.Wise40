using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ASCOM.Wise40.Common
{
    public class EnvironmentLogger
    {
        private string _stationName;
        private StreamWriter _sw;
        Dictionary<string, string> _fileNames = new Dictionary<string, string>();

        public EnvironmentLogger(string stationName)
        {
            _stationName = stationName;
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

            _sw.WriteLine(date.ToString(@"HH:mm.ss") + ',' + value);
            _sw.Flush();
        }
    }
}
