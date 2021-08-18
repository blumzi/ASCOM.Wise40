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
                { Severity.Normal, Const.NormalColor },
                { Severity.Warning, Const.WarningColor },
                { Severity.Error, Const.ErrorColor },
                { Severity.Good, Const.GoodColor },
            };
        private readonly ToolTip _toolTip;
        private DateTime _expiration;
        private readonly Timer _timer;
        private bool _busy = false;

        public Statuser(Label label, ToolTip tooltip = null)
        {
            this.Label = label;
            _timer = new Timer
            {
                Interval = 100
            };
            _timer.Tick += Tick;
            _toolTip = tooltip;
        }

        public Label Label { get; }
        public void Show(string s, int millis = 0, Severity severity = Severity.Normal, bool silent = false)
        {
            if (_busy)
                return;

            if (severity == Severity.Error && !silent)
                System.Media.SystemSounds.Beep.Play();

            if (s == null)
                return;

            if (Label != null)
            {
                Label.ForeColor = colors[severity];
                Label.Text = s;
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
            _toolTip.SetToolTip(Label, tip);
        }

        private void Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.CompareTo(_expiration) > 0)
            {
                Label.Text = "";
                _toolTip?.SetToolTip(Label, "");

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
                default:
                    return colors[Severity.Normal];
            }
        }
    }
}
