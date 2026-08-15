using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;      // Const.crnl, in the tooltips below
using RavSoft;

namespace ASCOM.Wise40 //.FilterWheel
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class FilterWheelSetupDialogForm : Form
    {
        private readonly WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;

        //
        // The filter at each position, keyed "<nPositions>:<position>".  These are
        //  plain labels: the filter is looked up in MaxIm DL and shown, never
        //  chosen here, so a dropdown would be offering a choice that does not
        //  exist.  See ReplaceFilterCombosWithLabels().
        //
        private readonly Dictionary<string, Label> filterLabels = new Dictionary<string, Label>();

        private static string LabelKey(WiseFilterWheel.Wheel w, int pos) => $"{w._nPositions}:{pos}";

        public FilterWheelSetupDialogForm()
        {
            wisefilterwheel.Init();

            WiseFilterWheel.ReadProfile();
            InitializeComponent();
            ReplaceFilterCombosWithLabels();
            InitUI();
        }

        /// <summary>
        /// Swaps each filter ComboBox for a Label in the same cell.
        ///
        /// The designer lays these out as combo boxes because filters used to be
        ///  picked here.  They are not any more - MaxIm DL says what is loaded -
        ///  so they become labels, in code, rather than by hand-editing 24 controls
        ///  out of a 930 line designer file.
        /// </summary>
        private void ReplaceFilterCombosWithLabels()
        {
            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    Control[] found = Controls.Find(string.Format("comboBox{0}{1}", w._nPositions, i), true);
                    if (found.Length == 0 || !(found[0] is ComboBox cb))
                        continue;

                    Label lbl = new Label
                    {
                        Name = string.Format("labelFilter{0}{1}", w._nPositions, i),
                        Font = cb.Font,
                        ForeColor = cb.ForeColor,
                        BackColor = cb.BackColor,
                        Location = cb.Location,
                        Size = cb.Size,
                        Margin = cb.Margin,
                        Anchor = cb.Anchor,
                        Dock = cb.Dock,
                        AutoSize = false,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoEllipsis = true,
                    };

                    Control parent = cb.Parent;
                    if (parent is TableLayoutPanel tlp)
                    {
                        //  Take the combo's cell, or the label lands wherever the
                        //  panel's next free cell happens to be.
                        TableLayoutPanelCellPosition cell = tlp.GetCellPosition(cb);
                        tlp.Controls.Remove(cb);
                        tlp.Controls.Add(lbl, cell.Column, cell.Row);
                    }
                    else
                    {
                        parent.Controls.Remove(cb);
                        parent.Controls.Add(lbl);
                    }
                    cb.Dispose();

                    filterLabels[LabelKey(w, i)] = lbl;
                }
            }
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Form F = this.FindForm();

            //
            // Only the RFID tags are ours to write.  The filter name, its
            //  description and its focus offset are read-only here - they belong to
            //  whatever the observer maintains elsewhere (ACP's FilterInfo.txt for
            //  the offsets, MaxIm for the names), so this dialog shows them and
            //  does not save them.
            //
            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    w._positions[i].tag = tb.Text ?? string.Empty;
                }
            }
            WiseFilterWheel.Instance.arduino.SerialPortName = comboBoxPort.Text;
            WiseFilterWheel.Enabled = checkBoxEnabled.Checked;

            WiseFilterWheel.WriteProfile();
            Close();
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            Form F = this.FindForm();
            WiseFilterWheel.ReadProfile();

            foreach (var w in WiseFilterWheel.wheels)
            {
                WiseFilterWheel.Instance.Init();
                for (int i = 0; i < w._nPositions; i++)
                {
                    //
                    // What used to be the two inventory windows lives here now: the
                    //  filter and its focus offset, with the detail and the source of
                    //  each in the tooltip.
                    //
                    if (filterLabels.TryGetValue(LabelKey(w, i), out Label lbl))
                    {
                        lbl.Text = FilterText(w, i);
                        toolTip1.SetToolTip(lbl, FilterTooltip(w, i));
                    }

                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBoxWheel{0}RFID{1}", w._nPositions, i), true)[0];
                    tb.Text = w._positions[i].tag ?? string.Empty;
                    tb.Enabled = checkBoxEditableRFIDs.Checked;
                }
            }

            string[] existingPorts = System.IO.Ports.SerialPort.GetPortNames();
            comboBoxPort.Items.AddRange(existingPorts);

            string port = WiseFilterWheel.Instance.arduino.SerialPortName;
            if (!String.IsNullOrEmpty(port))
            {
                foreach (var p in existingPorts)
                {
                    if (p == port)
                    {
                        comboBoxPort.Text = port;
                        break;
                    }
                }
            } else
            {
                comboBoxPort.Text = "";
            }

            labelOpModeValue.Text = WiseSite.OperationalMode.ToString();

            UpdateEditability();
        }

        /// <summary>
        /// What sits at a wheel position, on one line: the filter, and its focus
        ///  offset when anyone can tell us what it is.
        /// </summary>
        private static string FilterText(WiseFilterWheel.Wheel w, int pos)
        {
            //
            // The name is MaxIm's.  Both wheels read the same list from the start -
            //  the eight position wheel takes slots 1..8, the four position wheel
            //  slots 1..4 - because MaxIm keeps one list per camera and has no idea
            //  which of our wheels is mounted.
            //
            string filterName = MaxImFilterNames.ForPosition(pos);
            AcpFilterInfo.Entry acp = AcpFilterInfo.ForPosition(pos);

            //
            // Never render an empty cell.  An empty one is indistinguishable from a
            //  slot with no filter, from MaxIm not being set up, from us failing to
            //  find its file at all - and leaves whoever is looking at it with
            //  nothing to go on.  Say which it is.
            //
            if (string.IsNullOrEmpty(filterName))
                return $"<{MaxImFilterNames.Provenance}>";

            string offset = (acp != null) ? acp.Offset.ToString() : "?";

            return $"{filterName}  (offset {offset})";
        }

        /// <summary>
        /// The same, spelled out, with where each part of it came from.  Only the
        ///  RFID tag is ours - everything else is looked up in what the observer
        ///  maintains elsewhere, and may simply not be there.
        /// </summary>
        private static string FilterTooltip(WiseFilterWheel.Wheel w, int pos)
        {
            string filterName = MaxImFilterNames.ForPosition(pos);
            AcpFilterInfo.Entry acp = AcpFilterInfo.ForPosition(pos);

            string tip =
                $" Position: {pos + 1} of {w._nPositions}  (MaxIm slot {pos + 1})" + Const.crnl +
                $" Filter:   {(string.IsNullOrEmpty(filterName) ? "(MaxIm names no filter here)" : filterName)}" +
                    $"   [{MaxImFilterNames.Provenance}]" + Const.crnl +
                $" RFID tag: {(string.IsNullOrEmpty(w._positions[pos].tag) ? "(none)" : w._positions[pos].tag)}   [Wise40]";

            if (acp != null)
            {
                tip += Const.crnl + $" Offset:   {acp.Offset}   [{AcpFilterInfo.Provenance}]";
                tip += Const.crnl + $" Ref filt: {acp.ReferenceFilter}   Ptg filt: {acp.PointingFilter}";
                if (acp.AutofocusMinMag.HasValue || acp.AutofocusMaxMag.HasValue)
                    tip += Const.crnl + $" AF mags:  {acp.AutofocusMinMag} .. {acp.AutofocusMaxMag}";
            }
            else
            {
                tip += Const.crnl + " Offset:   ?   [ACP says nothing about this position]";
            }

            return tip;
        }

        private void UpdateEditability()
        {
            Form F = this.FindForm();

            if (WiseFilterWheel.Enabled)
            {
                checkBoxEnabled.Checked = true;
                checkBoxEditableRFIDs.Checked = false;
                comboBoxPort.Enabled = true;
            }
            else
            {
                checkBoxEnabled.Checked = false;
                checkBoxEnabled.AutoCheck = false;
                checkBoxEditableRFIDs.Checked = false;
                checkBoxEditableRFIDs.AutoCheck = false;
                comboBoxPort.Enabled = false;
            }

            bool editableRFIDs = checkBoxEditableRFIDs.Checked;
            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    //
                    // Nothing to do for the filter: it is a Label now, and a label
                    //  is never editable.  Only the RFID tag follows the "editable
                    //  RFIDs" checkbox.
                    //
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBoxWheel{0}RFID{1}", w._nPositions, i), true)[0];
                    tb.Enabled = editableRFIDs;
                }
            }
        }

        private void FilterWheelSetupDialogForm_Load(object sender, EventArgs e)
        {
            Form F = this.FindForm();

            foreach (var w in new List<WiseFilterWheel.Wheel> { WiseFilterWheel.wheel4, WiseFilterWheel.wheel8})
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                }
            }
        }

        private void checkBoxEditableRFIDs_CheckedChanged(object sender, EventArgs e)
        {
            Form F = this.FindForm();
            CheckBox cb = sender as CheckBox;

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                    tb.Enabled = cb.Checked;
                }
            }
        }
    }
}