using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    class SafetyMonitorTimer
    {
        private System.Threading.Timer timer;
        private bool _isOn;
        private int dueTime, period;

        public SafetyMonitorTimer(System.Threading.TimerCallback callback, int dueTime, int period)
        {
            timer = new System.Threading.Timer(callback);
            this.dueTime = dueTime;
            this.period = period;
            _isOn = false;
        }

        public bool isOn
        {
            get
            {
                return _isOn;
            }
        }

        public void SetOn()
        {
            timer.Change(dueTime, period);
            _isOn = true;
        }

        public void SetOff()
        {
            timer.Change(0, 0);
            _isOn = false;
        }
    }
}
