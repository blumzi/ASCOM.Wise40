using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Wise40Watcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            //
            // Says what killed us, on the way out.  It cannot PREVENT the termination - by design
            //  since .NET 2.0 - but the watcher died seven times before anyone knew why, and the
            //  only record was a Windows .NET Runtime event.  A line in our own log would have
            //  found it far sooner.
            //
            ASCOM.Wise40.Common.Guarded.InstallProcessHandlers("Wise40Watcher");

            ServiceBase[] ServicesToRun = new ServiceBase[] { new Wise40Watcher() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
