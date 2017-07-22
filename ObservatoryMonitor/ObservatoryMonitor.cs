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

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObsMainForm : Form
    {
        private ObsMon obsmon;
        public const int _maxLogItems = 1000;
        private Statuser statuser;
        List<Label> statusLights;

        public ObsMainForm()
        {
            InitializeComponent();
            obsmon = ObsMon.Instance;
            obsmon.init(this);
            statuser = new Statuser(labelStatus);
            statusLights = new List<Label> { labelSun, labelRain, labelWind, labelHumidity, labelClouds };
            listBoxLog.SelectionMode = SelectionMode.None;

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();
        }

        void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = localTime.ToLongDateString() + Const.crnl + Const.crnl + localTime.ToLongTimeString();

            if (obsmon.ShuttingDown)
                statuser.Show("Shutting down ...", 0, Statuser.Severity.Good);
            else if (!obsmon.Enabled)
            {
                statuser.Show("Manually disabled!", 0, Statuser.Severity.Warning);
                foreach (var light in statusLights)
                    light.ForeColor = Statuser.colors[Statuser.Severity.Warning];
            }
            else if (!obsmon.OnDuty)
            {
                statuser.Show("Out of duty, the Sun is up!", 0, Statuser.Severity.Error, silent: true);
                foreach (var light in statusLights)
                    light.ForeColor = Statuser.colors[Statuser.Severity.Error];
            }
            else
            {
                statuser.Show("Next check in " + obsmon.SecondsToNextCheck.ToString() + " seconds", 0, Statuser.Severity.Good);
                
                Update(labelSun, obsmon.sunIsSafe);
                Update(labelLight, obsmon.lightIsSafe);
                Update(labelRain, obsmon.rainIsSafe);
                Update(labelWind, obsmon.windIsSafe);
                Update(labelClouds, obsmon.cloudsAreSafe);
                Update(labelHumidity, obsmon.humidityIsSafe);
            }

            buttonPark.Enabled = !obsmon.ShuttingDown;
            buttonEnable.Enabled = !obsmon.ShuttingDown;
        }

        private void Update(Label label, Func<Const.TriStateStatus> func)
        {
            Const.TriStateStatus stat = func();
            label.BackColor = Statuser.TriStateColor(stat);
            string tip;
            
            if (stat == Const.TriStateStatus.Normal)
                tip  = "Disabled by settings";
            else if (stat == Const.TriStateStatus.Good)
                tip = "Threshold not exceeded";
            else if (stat == Const.TriStateStatus.Warning)
                tip = "Cannot read sensor";
            else
                tip = "Last reading was over the threshold";
            toolTip.SetToolTip(label, tip);
        }

        private void timerDisplayRefresh_Tick(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetupDialogForm setup = new SetupDialogForm();
            setup.Visible = true;
        }

        delegate void LogDelegate(string text);

        public void Log(string fmt, params object[] o)
        {
            string line = string.Format("{0} - {1}", DateTime.Now.ToString("dd MMMM, yyyy H:mm:ss"), string.Format(fmt, o));

            log(line);
        }

        public void log(string line)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (listBoxLog.InvokeRequired)
            {
                LogDelegate _log = new LogDelegate(log);
                this.Invoke(_log, new object[] { line });
            }
            else
            {
                if (listBoxLog.Items.Count > _maxLogItems)
                    listBoxLog.Items.RemoveAt(0);
                listBoxLog.Items.Add(line);
            }

            string dir = string.Format("c:/Logs/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dir);
            using (var sw = new StreamWriter(dir + "/ObservatoryMonitor.log", true))
            {
                sw.WriteLine(line);
            }
        }

        private void buttonEnable_Click(object sender, EventArgs e)
        {
            obsmon.Enabled = !obsmon.Enabled;
            buttonEnable.Text = (obsmon.Enabled ? "Disable" : "Enable") + " Monitoring";
            if (!obsmon.Enabled)
                labelStatus.Text = "Manually disabled";
        }

        private void buttonPark_Click(object sender, EventArgs e)
        {
            obsmon.Enabled = false;
            obsmon.ParkAndClose();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form about = new AboutForm(obsmon);
            about.Show();
        }
    }
}