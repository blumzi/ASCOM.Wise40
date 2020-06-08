using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class InterventionForm : Form
    {
        private static string _operator = string.Empty;

        public InterventionForm()
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(_operator))
                textBoxOperator.Text = _operator;

            cmdOK.DialogResult = DialogResult.OK;
            cmdCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = cmdOK;
            CancelButton = cmdCancel;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            _operator = textBoxOperator.Text;
            HumanIntervention.Create(_operator, textBoxReason.Text, checkBoxGlobal.Checked);
            Close();
        }
    }
}
