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
            ServiceBase[] ServicesToRun = new ServiceBase[] { new Wise40Watcher() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
