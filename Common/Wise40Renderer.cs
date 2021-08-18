using System.Windows.Forms;
using System.Drawing;

namespace ASCOM.Wise40.Common
{
    public class Wise40ToolstripRenderer : System.Windows.Forms.ToolStripRenderer
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Const.NormalColor;
            e.ToolStrip.BackColor = Color.FromArgb(64, 64, 64);
            base.OnRenderItemText(e);
        }
    }
}