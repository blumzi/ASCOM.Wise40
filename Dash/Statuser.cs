using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Dash
{
    class Statuser
    {
        public enum Severity { Normal, Good, Warning, Error };
        public static Dictionary<Severity, Color> colors = new Dictionary<Severity, Color>()
            {
                { Severity.Normal, Color.FromArgb(176, 161, 142) },
                { Severity.Warning, Color.LightGoldenrodYellow },
                { Severity.Error, Color.IndianRed },
                { Severity.Good, Color.Green },
            };

        private Label label;
        private ToolStripStatusLabel toolStripStatusLabel;
        private DateTime expiration;
        private System.Windows.Forms.Timer timer;

        public Statuser(Label label)
        {
            this.label = label;
            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Tick;
        }

        public Statuser(ToolStripStatusLabel label)
        {
            this.toolStripStatusLabel = label;
            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Tick;
        }

        public void Show(string s, int millis = 0, Severity severity = Severity.Normal)
        {
            if (severity == Severity.Error)
                System.Media.SystemSounds.Beep.Play();

            if (s == null)
                return;

            if (label != null)
            {
                label.ForeColor = colors[severity];
                label.Text = s;
            } else if (toolStripStatusLabel != null)
            {
                toolStripStatusLabel.ForeColor = colors[severity];
                toolStripStatusLabel.Text = s;
            }

            if (millis > 0)
            {
                expiration = DateTime.Now.AddMilliseconds(millis);
                timer.Start();
            }
        }

        private void Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.CompareTo(expiration) > 0)
            {
                label.Text = "";
                expiration = now;
                timer.Stop();
            }
        }
    }
}
