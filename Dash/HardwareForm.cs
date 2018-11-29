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
    public partial class HardwareForm : Form
    {
        private List<WiseBoard> boards;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;
        private WiseTele wisetele = WiseTele.Instance;
        private WiseDome wisedome = WiseDome.Instance;
        private WiseFocuser wisefocuser = WiseFocuser.Instance;
        private WiseSite wisesite = WiseSite.Instance;
        private WiseDomePlatform wisedomeplatform = WiseDomePlatform.Instance;

        public HardwareForm()
        {
            InitializeComponent();
            hw.init();
            wisetele.init();
            wisetele.Connected = true;
            wisedome.Connected = true;
            wisefocuser.Connected = true;

            boards = new List<WiseBoard>();
            boards.Add(hw.miscboard);
            boards.Add(hw.teleboard);
            boards.Add(hw.domeboard);

            Control[] controls;

            foreach (WiseBoard board in boards)
            {
                controls = Controls.Find("gbBoard" + board.boardNum, true);
                if (controls.Count() == 1)
                {
                    board.gb = (GroupBox)controls[0];
                    if (board.type == WiseBoard.BoardType.Soft)
                        board.gb.Text += " [Simulated]";
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
                            string s = "[" + bit.ToString() + "] " + daq.owners[bit].owner;
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

        private void timerHardwareRefresh_Tick(object sender, EventArgs e)
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

        private void HardwareForm_VisibleChanged(object sender, EventArgs e)
        {
            Form form = sender as Form;

            timerHardwareRefresh.Enabled = form.Visible;
        }
    }
}
