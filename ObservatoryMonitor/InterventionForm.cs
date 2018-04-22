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
        static string _operator = string.Empty;

        public InterventionForm()
        {
            InitializeComponent();
            if (_operator != string.Empty)
                textBoxOperator.Text = _operator;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            _operator = textBoxOperator.Text;
            using (StreamWriter sw = new StreamWriter(Const.humanInterventionFilePath))
            {
                sw.WriteLine("Operator: " + textBoxOperator.Text);
                sw.WriteLine("Reason: " + textBoxReason.Text);
                sw.WriteLine("Created: " + DateTime.Now.ToString());
            }
            Close();
        }
    }
}
