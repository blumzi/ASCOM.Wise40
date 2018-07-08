using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

using System.ServiceProcess;

namespace Wise40Watcher
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
        public ProjectInstaller()
        {
            InitializeComponent();
            processInstaller.Account = ServiceAccount.LocalSystem;

            Installers.Add(processInstaller);
        }
    }
}
