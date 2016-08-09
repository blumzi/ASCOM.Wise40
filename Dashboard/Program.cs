using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Astrometry;

namespace Dashboard
{
    class Program
    {
        private static WiseTele wisetele = WiseTele.Instance;
        private static WiseDome wisedome = WiseDome.Instance;
        private static WiseSite wisesite = WiseSite.Instance;
        private static Debugger debugger = Debugger.Instance;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            wisetele.init();
            wisedome.init();
            wisesite.init();
            debugger.init();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DashboardForm());
        }
    }
}
