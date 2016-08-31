using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{

    /// <summary>
    /// A timer that should be on whenever any of the directional or tracking motors 
    ///  are on.  The callback checks if the telescope is safe at the current coordinates.
    /// </summary>
    class SafetyMonitorTimer
    {
        private static WiseTele wisetele = WiseTele.Instance;
        private System.Threading.Timer timer;
        private int _dueTime, _period;
        private bool _enabled;

        private void SafetyChecker(object StateObject)
        {
            wisetele.CheckSafetyAtCoordinates(
                Angle.FromHours(wisetele.RightAscension, Angle.Type.RA),
                Angle.FromDegrees(wisetele.Declination, Angle.Type.Dec),
                true);
        }

        public SafetyMonitorTimer(int dueTime = 100, int period = 100)
        {
            System.Threading.TimerCallback callback = new System.Threading.TimerCallback(SafetyChecker);

            timer = new System.Threading.Timer(callback);
            this._dueTime = dueTime;
            this._period = period;
            _enabled = false;
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
                if (_enabled)
                    timer.Change(_dueTime, _period);
                else
                    timer.Change(0, 0);
            }
        }

        public void EnableIfNeeded()
        {
            if ((wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.isOn) && !Enabled)
            {
                Enabled = true;
            }
        }

        public void DisableIfNotNeeded()
        {
            if (Enabled && !(wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.isOn))
            {
                Enabled = false;
            }
        }
    }
}
