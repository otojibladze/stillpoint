using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        static readonly Color RoamGreen = SpiritArt.Hex(0x6F8F58);
        readonly ToolTip roamTip = new ToolTip();
        bool roamHover;

        RectangleF RoamCircle
        {
            get
            {
                float size = Math.Min(Width, Height), s = size / 300f, r = 31 * s;
                var o = Origin;
                float cx = o.X + 262 * s, cy = Math.Min(o.Y + 268 * s, Height - r - 3 * s);
                return new RectangleF(cx - r, cy - r, r * 2, r * 2);
            }
        }

        static string RoamText(double seconds)
        {
            int total = (int)Math.Ceiling(Math.Max(0, seconds));
            return total >= 3600 ? Fmt.Clock(total) : $"{total / 60}:{total % 60:00}";
        }

        void DrawRoam(Graphics g)
        {
            var roam = state.Roam;
            if (!roam.Shown || Width < 60) return;
            var c = RoamCircle;
            float s = Math.Min(Width, Height) / 300f, cx = c.X + c.Width / 2, cy = c.Y + c.Height / 2;
            var back = Parent?.BackColor ?? Theme.Background;
            using (var gap = new SolidBrush(back)) g.FillEllipse(gap, RectangleF.Inflate(c, 4 * s, 4 * s));
            using (var fill = new SolidBrush(Theme.Surface)) g.FillEllipse(fill, c);
            float progress = roam.Running ? (float)roam.ToNext : roam.Full > 0 ? (float)(roam.Left / roam.Full) : 0;
            var arc = roam.Running ? SpiritArt.Alpha(RoamGreen, 0.45f) : RoamGreen;
            Draw.Ring(g, RectangleF.Inflate(c, -1.5f * s, -1.5f * s), 3.4f * s, Theme.Track, arc, SpiritArt.Clamp(progress, 0, 1));
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            bool empty = roam.Left < 0.5;
            CenteredText(g, "FREE ROAM", roamCapFont, Theme.Muted, cx, cy - 9.5f * s);
            CenteredText(g, RoamText(roam.Left), roamFont, empty && !roam.Running ? Theme.AccentText : Theme.Ink, cx, cy + 4 * s);
        }

        void HoverRoam(Point at)
        {
            var c = RoamCircle;
            float dx = at.X - (c.X + c.Width / 2), dy = at.Y - (c.Y + c.Height / 2);
            bool inside = state.Roam.Shown && dx * dx + dy * dy <= c.Width * c.Width / 4;
            if (inside == roamHover) return;
            roamHover = inside;
            roamTip.SetToolTip(this, inside ? "Free roam: every 30 minutes you sit today earns 5 minutes away.\nIt counts down while you're up and starts fresh each day." : null);
        }
    }
}
