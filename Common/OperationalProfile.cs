using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public class OperationalProfile
    {
        private WiseSite.OpMode _opMode;

        public OperationalProfile(WiseSite.OpMode opMode)
        {
            _opMode = opMode;
        }

        public WiseSite.OpMode OpMode
        {
            get
            {
                return _opMode;
            }

            set
            {
                _opMode = value;
            }
        }

        public bool EnslavesDome
        {
            get
            {
                return _opMode == WiseSite.OpMode.ACP ? false : true;
            }
        }

        public bool CalculatesRefractionForHorizCoords
        {
            get
            {
                return true;
            }
        }

        public bool UsesApparentCoords
        {
            get
            {
                return _opMode == WiseSite.OpMode.LCO;
            }
        }

        public ASCOM.DeviceInterface.EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                switch (_opMode)
                {
                    case WiseSite.OpMode.ACP:
                        return DeviceInterface.EquatorialCoordinateType.equTopocentric;
                    case WiseSite.OpMode.WISE:
                        return DeviceInterface.EquatorialCoordinateType.equJ2000;
                    case WiseSite.OpMode.LCO:
                        return DeviceInterface.EquatorialCoordinateType.equTopocentric;
                    default:
                        return DeviceInterface.EquatorialCoordinateType.equOther;
                }
            }
        }
    }
}
