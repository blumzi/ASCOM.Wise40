using System;
using System.Windows.Forms;

using ASCOM.Wise40.Common;

namespace Dash
{
    public partial class AboutForm : Form
    {
        FormDash _dash;
        Version version = new Version(0, 2);
        string urlCommit, urlRelease;

        public AboutForm(FormDash dashForm)
        {
            _dash = dashForm;
            InitializeComponent();
            
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
                "Author: Arie Blumenzweig <theblumz@gmail.com>" + Const.crnl +
                "since October 2016" + Const.crnl;

            labelDashVersion.Text = appVersion;
            labelTelescopeVersion.Text = _dash.wiseTelescope.DriverVersion;
            labelDomeVersion.Text = _dash.wiseDome.DriverVersion;
            labelFocuserVersion.Text = _dash.wiseFocuser.DriverVersion;
            labelSafeToOperateVersion.Text = _dash.wiseSafeToOperate.DriverVersion;
            labelBoltwoodVersion.Text = _dash.wiseBoltwood.DriverVersion;
            labelVantageProVersion.Text = ASCOM.Wise40.VantagePro.WiseVantagePro.DriverVersion;
            labelFilterWheelVersion.Text = _dash.wiseFilterWheel.DriverVersion;

            linkLabelLatestCommit.Text = $"{Properties.Resources.CurrentCommitShort}";
            linkLabelRelease.Text = $"{Properties.Resources.RemoteTag}";

            string repo = $"{Properties.Resources.RemoteUrl.Replace(".git", "")}";
            urlCommit = $"{repo}/commit/{Properties.Resources.CurrentCommitLong}";
            urlRelease = $"{repo}/releases/tag/{Properties.Resources.RemoteTag}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabelVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(urlRelease);
        }

        private void linkLabelLatestCommit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(urlCommit);
        }
    }
}
