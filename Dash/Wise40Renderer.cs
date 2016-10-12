using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

namespace Dash
{
    public class Wise40ToolstripRenderer : System.Windows.Forms.ToolStripRenderer
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(176, 161, 142);
            e.ToolStrip.BackColor = Color.FromArgb(64, 64, 64);
            base.OnRenderItemText(e);
        }
    }
}
