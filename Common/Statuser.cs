using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


namespace ASCOM.Wise40.Common
{
    public class Statuser
    {
        public enum Severity { Normal, Good, Warning, Error };
        public static Dictionary<Severity, Color> colors = new Dictionary<Severity, Color>()
            {
                { Severity.Normal, Color.FromArgb(176, 161, 142) },
                { Severity.Warning, Color.LightYellow },
                { Severity.Error, Color.IndianRed },
                { Severity.Good, Color.Green },
            };

        private Label _label;
        private ToolTip _toolTip;
        private DateTime _expiration;
        private System.Windows.Forms.Timer _timer;
        private bool _busy = false;

        public Statuser(Label label, ToolTip tooltip = null)
        {
            this._label = label;
            _timer = new Timer();
            _timer.Interval = 100;
            _timer.Tick += Tick;
            _toolTip = tooltip;
        }

        public void Show(string s, int millis = 0, Severity severity = Severity.Normal, bool silent = false)
        {
            if (_busy)
                return;

            if (severity == Severity.Error && !silent)
                System.Media.SystemSounds.Beep.Play();

            if (s == null)
                return;

            if (_label != null)
            {
                _label.ForeColor = colors[severity];
                _label.Text = s;
            }

            if (millis > 0)
            {
                _expiration = DateTime.Now.AddMilliseconds(millis);
                _timer.Start();
                _busy = true;
            }
        }

        public void SetToolTip(string tip)
        {
            _toolTip.SetToolTip(_label, tip);
        }

        private void Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.CompareTo(_expiration) > 0)
            {
                _label.Text = "";
                if (_toolTip != null)
                    _toolTip.SetToolTip(_label, "");
                _expiration = now;
                _timer.Stop();
                _busy = false;
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
