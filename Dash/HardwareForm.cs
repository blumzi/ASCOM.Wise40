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
using Newtonsoft.Json;

namespace ASCOM.Wise40
{
    public partial class HardwareForm : Form
    {
        private readonly List<BoardControl> BoardControls;
        public DriverAccess.Telescope _tele;

        public HardwareForm(DriverAccess.Telescope tele)
        {
            InitializeComponent();

            _tele = tele;

            HardwareMetaDigest meta = JsonConvert.DeserializeObject<HardwareMetaDigest>(
                                                                _tele.Action("hardware-meta-digest", ""));
            BoardControls = new List<BoardControl>();

            foreach (BoardMetaDigest mb in meta.Boards)
                BoardControls.Add(new BoardControl(mb, this));
        }

        public void timerHardwareRefresh_Tick(object sender, EventArgs e)
        {
            HardwareDigest hw = JsonConvert.DeserializeObject<HardwareDigest>(_tele.Action("hardware-digest", ""));

            for (int b = 0; b < BoardControls.Count; b++)
            {
                for (int d = 0; d < BoardControls[b].DaqControls.Count; d++)
                {
                    ushort val = hw.Boards[b].Daqs[d].Value;

                    for (int bit = 0; bit < BoardControls[b].DaqControls[d].nbits; bit++)
                    {
                        if (BoardControls[b].DaqControls[d].cbs[bit] != null)
                            BoardControls[b].DaqControls[d].cbs[bit].Checked = (val & (1 << bit)) != 0;
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

    public class DaqControl
    {
        public CheckBox[] cbs;
        public GroupBox gb;
        public int nbits;

        public DaqControl(DaqMetaDigest md, BoardMetaDigest mb, Form form)
        {
            string controlName;
            string portType = md.Porttype.ToString();

            if (portType.EndsWith("C"))
                portType += "L";

            controlName = "gbBoard" + mb.Number + portType;

            Control[] found = form.Controls.Find(controlName, true);

            if (found.Length != 1)
                return;

            gb = (GroupBox)found[0];
            gb.Text = md.Porttype + ((md.Portdir == MccDaq.DigitalPortDirection.DigitalIn) ? " [I]" : " [O]");

            nbits = md.Nbits;
            cbs = new CheckBox[nbits];
            for (int bit = 0; bit < nbits; bit++)
            {
                controlName = "cb" +
                    "Board" + mb.Number.ToString() +
                    portType +
                    "bit" + bit.ToString();

                found = form.Controls.Find(controlName, true);
                if (found.Length != 1)
                    continue;

                cbs[bit] = (CheckBox)found[0];

                string s = "[" + bit.ToString() + "] " + md.Owners[bit];
                int idx = s.IndexOf('@');
                if (idx != -1)
                    s = s.Remove(idx);

                cbs[bit].Text = s;
            }
        }
    }

    public class BoardControl
    {
        public GroupBox gb;
        public List<DaqControl> DaqControls;

        public BoardControl(BoardMetaDigest mb, Form form)
        {
            Control[] controls = form.Controls.Find("gbBoard" + mb.Number, true);
            if (controls.Length == 1)
            {
                gb = (GroupBox)controls[0];
                if (mb.Type == WiseBoard.BoardType.Soft)
                    gb.Text += " [Simulated]";
            }

            DaqControls = new List<DaqControl>(mb.Daqs.Count);
            foreach (DaqMetaDigest md in mb.Daqs)
                DaqControls.Add(new DaqControl(md, mb, form));
        }
    }
}
