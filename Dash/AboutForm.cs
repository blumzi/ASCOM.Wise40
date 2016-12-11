using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using ASCOM.Wise40.SafeToOperate;
using ASCOM.DriverAccess;

namespace Dash
{
    public partial class AboutForm : Form
    {
        FormDash _dash;

        public AboutForm(FormDash dashForm)
        {
            _dash = dashForm;
            InitializeComponent();

            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string appVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);

            labelAbout.Text = "This dashboard controls the Wise 40 inch Observatory at" + Const.crnl +
                "Mizpe Ramon, Israel." + Const.crnl +
                Const.crnl +
                "The Wise Observatory is operated by the" + Const.crnl +
                Const.crnl +
                "Department of Astrophysics" + Const.crnl +
                "School of Physics and Astronomy" + Const.crnl +
                "Raymond and Beverly Sackler Faculty of Exact Sciences" + Const.crnl +
                Const.crnl +
                "Tel Aviv University" + Const.crnl +
                Const.crnl +
                "Author: Arie Blumenzweig <blumzi@013.net>" + Const.crnl +
                "since October 2016" + Const.crnl;

            labelDashVersion.Text = appVersion;
            labelTelescopeVersion.Text = _dash.wisetele.DriverVersion;
            labelDomeVersion.Text = _dash.wisedome.DriverVersion;
            labelFocuserVersion.Text = _dash.wisefocuser.DriverVersion;
            labelSafeToOpenVersion.Text = _dash.wisesite.safeToOpen.DriverVersion;            
            labelBoltwoodVersion.Text = _dash.boltwood.DriverVersion;
            labelComputerControlVersion.Text = _dash.wisesite.computerControl.DriverVersion;
            labelVantageProVersion.Text = (new ObservingConditions("ASCOM.Vantage.ObservingConditions")).DriverVersion;
            labelFilterWheelVersion.Text = _dash.wisefilterwheel.DriverVersion;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
