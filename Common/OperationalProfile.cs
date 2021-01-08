using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Utilities;

namespace ASCOM.Wise40.Common
{
    public class OperationalProfile
    {
        public OperationalProfile()
        {
            if (WiseSite.ObservatoryName != "wise40")
                return;

            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                if (Enum.TryParse<WiseSite.OpMode>(driverProfile.GetValue(Const.WiseDriverID.Telescope,"SiteOperationMode", null, "WISE").ToUpper(), out WiseSite.OpMode mode))
                    OpMode = mode;
            }
        }

        public WiseSite.OpMode OpMode { get; set; } = WiseSite.OpMode.NONE;

        public bool EnslavesDome
        {
            get
            {
                return OpMode == WiseSite.OpMode.LCO || OpMode == WiseSite.OpMode.WISE;
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
                return OpMode == WiseSite.OpMode.LCO;
            }
        }

        public ASCOM.DeviceInterface.EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                switch (OpMode)
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
