using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40
{
    public partial class DaqsForm : Form
    {
        private List<WiseBoard> boards;
        private HandpadForm handpad;

        public DaqsForm(HandpadForm handpad)
        {
            InitializeComponent();

            StartPosition = FormStartPosition.Manual;
            Location = new Point(handpad.Top, handpad.Right);
            this.handpad = handpad;

            boards = new List<WiseBoard>();
            boards.Add(Hardware.Hardware.Instance.miscboard);
            boards.Add(Hardware.Hardware.Instance.teleboard);
            boards.Add(Hardware.Hardware.Instance.domeboard);

            Control[] controls;

            foreach (WiseBoard board in boards)
            {
                controls = Controls.Find("gbBoard" + board.boardNum, true);
                if (controls.Count() == 1)
                {
                    board.gb = (GroupBox)controls[0];
                    board.gb.Text += (board.type == WiseBoard.BoardType.Hard) ? " [Hard]" : " [Soft]";
                }
                else
                    Console.WriteLine("Missing: {0}", "gbBoard" + board.boardNum);

                foreach (WiseDaq daq in board.daqs)
                {
                    string porttype = daq.porttype.ToString();
                    if (porttype.EndsWith("C"))
                        porttype += "L";

                    controls = Controls.Find("gbBoard" + board.boardNum + porttype, true);
                    if (controls.Count() == 1)
                    {
                        daq.gb = (GroupBox)controls[0];
                        daq.gb.Text = porttype + ((daq.portdir == MccDaq.DigitalPortDirection.DigitalIn) ? " [I]" : " [O]");
                    }
                    else
                        Console.WriteLine("Missing: {0}", "gbBoard" + board.boardNum + porttype);

                    for (int bit = 0; bit < daq.nbits; bit++)
                    {
                        controls = Controls.Find("cb" + "Board" + board.boardNum.ToString() + porttype + "bit" + bit.ToString(), true);
                        if (controls.Count() == 1)
                            daq.owners[bit].checkBox = (CheckBox)controls[0];
                        else
                            Console.WriteLine("Missing: {0}", "cb" + "Board" + board.boardNum.ToString() + porttype + "bit" + bit.ToString());
                        if (daq.owners[bit].checkBox != null)
                        {
                            string s = daq.owners[bit].owner;
                            if (s != null)
                            {
                                int idx = s.IndexOf('@');
                                if (idx != -1)
                                    s = s.Remove(idx);
                            }

                            daq.owners[bit].checkBox.Text = ((s == null) ? "" : s);
                        }
                    }
                }
            }
        }

        private void timerDaqsRefresh_Tick(object sender, EventArgs e)
        {
            foreach (WiseBoard board in boards)
            {
                foreach (WiseDaq daq in board.daqs)
                {
                    ushort val;

                    val = daq.Value;
                    for (int bit = 0; bit < daq.nbits; bit++)
                    {
                        if (daq.owners[bit].checkBox != null)
                        {
                            daq.owners[bit].checkBox.Checked = ((val & (1 << bit)) == 0) ? false : true;
                        }
                    }
                }
            }
        }

        private void DaqsForm_VisibleChanged(object sender, EventArgs e)
        {
            timerDaqsRefresh.Enabled = ((Form)sender).Visible;
            if (!((Form)sender).Visible)
                handpad.buttonHardware.Text = "Show Hardware";
        }
    }
}
