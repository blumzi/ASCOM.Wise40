using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using ASCOM.Wise40.Common;

namespace Dash
{
    class Statuser
    {
        public enum Severity { Normal, Good, Warning, Error };
        public static Dictionary<Severity, Color> colors = new Dictionary<Severity, Color>()
            {
                { Severity.Normal, Color.FromArgb(176, 161, 142) },
                { Severity.Warning, Color.LightYellow },
                { Severity.Error, Color.IndianRed },
                { Severity.Good, Color.Green },
            };

        private Label label;
        private ToolTip toolTip;
        private DateTime expiration;
        private System.Windows.Forms.Timer timer;

        public Statuser(Label label, ToolTip tooltip = null)
        {
            this.label = label;
            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Tick;
            toolTip = tooltip;
        }

        public void Show(string s, int millis = 0, Severity severity = Severity.Normal, bool silent = false)
        {
            if (severity == Severity.Error && !silent)
                System.Media.SystemSounds.Beep.Play();

            if (s == null)
                return;

            if (label != null)
            {
                label.ForeColor = colors[severity];
                label.Text = s;
            }

            if (millis > 0)
            {
                expiration = DateTime.Now.AddMilliseconds(millis);
                timer.Start();
            }
        }

        public void SetToolTip(string tip)
        {
            toolTip.SetToolTip(label, tip);
        }

        private void Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.CompareTo(expiration) > 0)
            {
                label.Text = "";
                if (toolTip != null)
                    toolTip.SetToolTip(label, "");
                expiration = now;
                timer.Stop();
            }
        }

        public static Color TriStateColor(Const.TriStateStatus stat)
        {
            switch (stat)
            {
                case Const.TriStateStatus.Error:
                    return colors[Severity.Error];
                case Const.TriStateStatus.Good:
                    return colors[Severity.Good];
                case Const.TriStateStatus.Warning:
                    return colors[Severity.Warning];
            }
            return colors[Severity.Normal];
        }
    }
}
