using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.SafeToOperate;
using ASCOM.Wise40.Telescope;
using ASCOM.Wise40.Dome;
using ASCOM.Wise40;
using ASCOM.Utilities;
using ASCOM;

using System.Net;
using System.Net.Http;
using System.IO;

namespace ObservatoryMonitor
{
    public partial class Main : Form
    {
        internal static string driverID = "ASCOM.Wise40.ObservatoryMonitor";
        private int _interval;
        private int _sunEvents, _rainEvents, _windEvents, _humidityEvents, _lightEvents;

        private string intervalProfileName = "Interval";
        private string lightEventsProfileName = "LightEvents";
        private string sunEventsProfileName = "SunEvents";
        private string windEventsProfileName = "WindEvents";
        private string rainEventsProfileName = "RainEvents";
        private string humidityEventsProfileName = "HumidityEvents";

        private int _defaultInterval = 30;
        private int _defaultLightEvents = 3;
        private int _defaultSunEvents = 2;
        private int _defaultWindEvents = 4;
        private int _defaultRainEvents = 2;
        private int _defaultHumidityEvents = 2;

        private const int _maxLogItems = 1000;

        string wise40Url = "http://localhost:11111";

        public Main()
        {
            InitializeComponent();
            ReadProfile();
            timerCheckStatus.Interval = _interval;
            timerCheckStatus.Enabled = true;
        }

        void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = localTime.ToLongDateString() + Const.crnl + Const.crnl + localTime.ToLongTimeString();
        }

        private void timerDisplayRefresh_Tick(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void timerCheckStatus_Tick(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                _interval = Convert.ToInt32(driverProfile.GetValue(driverID, intervalProfileName, string.Empty, _defaultInterval.ToString()));
                _lightEvents = Convert.ToInt32(driverProfile.GetValue(driverID, lightEventsProfileName, string.Empty, _defaultLightEvents.ToString()));
                _sunEvents = Convert.ToInt32(driverProfile.GetValue(driverID, sunEventsProfileName, string.Empty, _defaultSunEvents.ToString()));
                _windEvents = Convert.ToInt32(driverProfile.GetValue(driverID, windEventsProfileName, string.Empty, _defaultWindEvents.ToString()));
                _rainEvents = Convert.ToInt32(driverProfile.GetValue(driverID, rainEventsProfileName, string.Empty, _defaultRainEvents.ToString()));
                _humidityEvents = Convert.ToInt32(driverProfile.GetValue(driverID, humidityEventsProfileName, string.Empty, _defaultHumidityEvents.ToString()));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, intervalProfileName, _interval.ToString());
                driverProfile.WriteValue(driverID, lightEventsProfileName, _lightEvents.ToString());
                driverProfile.WriteValue(driverID, sunEventsProfileName, _sunEvents.ToString());
                driverProfile.WriteValue(driverID, windEventsProfileName, _windEvents.ToString());
                driverProfile.WriteValue(driverID, rainEventsProfileName, _rainEvents.ToString());
                driverProfile.WriteValue(driverID, humidityEventsProfileName, _humidityEvents.ToString());
            }
        }

        private void ParkAndClose()
        {
            WiseTele wisetele = WiseTele.Instance;
            WiseDome wisedome = WiseDome.Instance;

            wisetele.init();
            wisedome.init();

            wisetele.Park();
            if (!wisetele._enslaveDome)
                wisedome.Park();
            wisedome.CloseShutter();
        }

        private void log(string fmt, params object[] o)
        {
            DateTime now = DateTime.Now;
            string msg = string.Format(fmt, o);
            string line = string.Format("{0}/{1}/{2} {3} {4}", now.Day, now.Month, now.Year, now.TimeOfDay, msg);

            if (listBoxLog.Items.Count > _maxLogItems)
            {
                listBoxLog.Items.RemoveAt(0);
            }
            listBoxLog.Items.Add(line);

            // TODO - Log to file
        }

        private string httpGet(string url)
        {
            string html = string.Empty;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                return html;
            } catch (Exception ex)
            {
                return "exception: " + ex.Message;
            }
        }
    }
}
