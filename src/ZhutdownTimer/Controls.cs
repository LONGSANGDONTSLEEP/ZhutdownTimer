using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class ThemePalette
    {
        public bool IsDark;
        public Color Window;
        public Color Surface;
        public Color SurfaceAlt;
        public Color Border;
        public Color Text;
        public Color Muted;
        public Color Accent;
        public Color AccentHover;
        public Color AccentSoft;
        public Color Danger;
        public Color Success;

        public static ThemePalette Create(bool dark)
        {
            if (dark)
            {
                return new ThemePalette
                {
                    IsDark = true,
                    Window = Color.FromArgb(15, 23, 42),
                    Surface = Color.FromArgb(30, 41, 59),
                    SurfaceAlt = Color.FromArgb(51, 65, 85),
                    Border = Color.FromArgb(71, 85, 105),
                    Text = Color.FromArgb(241, 245, 249),
                    Muted = Color.FromArgb(148, 163, 184),
                    Accent = Color.FromArgb(59, 130, 246),
                    AccentHover = Color.FromArgb(96, 165, 250),
                    AccentSoft = Color.FromArgb(30, 64, 175),
                    Danger = Color.FromArgb(248, 113, 113),
                    Success = Color.FromArgb(52, 211, 153)
                };
            }

            return new ThemePalette
            {
                IsDark = false,
                Window = Color.FromArgb(245, 247, 251),
                Surface = Color.White,
                SurfaceAlt = Color.FromArgb(241, 245, 249),
                Border = Color.FromArgb(226, 232, 240),
                Text = Color.FromArgb(30, 41, 59),
                Muted = Color.FromArgb(100, 116, 139),
                Accent = Color.FromArgb(37, 99, 235),
                AccentHover = Color.FromArgb(29, 78, 216),
                AccentSoft = Color.FromArgb(219, 234, 254),
                Danger = Color.FromArgb(220, 38, 38),
                Success = Color.FromArgb(5, 150, 105)
            };
        }
    }

    internal static class UiShape
    {
        public static GraphicsPath Rounded(Rectangle rectangle, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class CardPanel : Panel
    {
        public Color BorderColor { get; set; }
        public int Radius { get; set; }

        public CardPanel()
        {
            DoubleBuffered = true;
            Radius = 16;
            Padding = new Padding(22);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiShape.Rounded(rect, Radius))
            using (var fill = new SolidBrush(BackColor))
            using (var border = new Pen(BorderColor))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
        }
    }

    internal sealed class ModernButton : Button
    {
        private bool hovered;
        public bool Primary { get; set; }
        public bool Danger { get; set; }
        public ThemePalette Palette { get; set; }

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Height = 42;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            Color background;
            Color foreground;
            if (!Enabled)
            {
                background = p.SurfaceAlt;
                foreground = p.Muted;
            }
            else if (Primary)
            {
                background = hovered ? p.AccentHover : p.Accent;
                foreground = Color.White;
            }
            else if (Danger)
            {
                background = hovered ? Color.FromArgb(40, p.Danger) : p.SurfaceAlt;
                foreground = p.Danger;
            }
            else
            {
                background = hovered ? p.AccentSoft : p.SurfaceAlt;
                foreground = p.Text;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UiShape.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 9))
            using (var brush = new SolidBrush(background))
            {
                e.Graphics.FillPath(brush, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class ModernComboBox : ComboBox
    {
        public ThemePalette Palette { get; set; }

        public ModernComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            ItemHeight = 24;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            int index = e.Index >= 0 ? e.Index : SelectedIndex;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected ? p.AccentSoft : p.SurfaceAlt;
            Color foreground = Enabled ? p.Text : p.Muted;
            using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
            if (index >= 0 && index < Items.Count)
            {
                Rectangle textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 8), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, GetItemText(Items[index]), Font, textBounds, foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus) e.DrawFocusRectangle();
        }
    }

    internal sealed class FlatProgressBar : Control
    {
        private int value;
        public ThemePalette Palette { get; set; }
        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public FlatProgressBar()
        {
            Height = 8;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle full = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiShape.Rounded(full, Height / 2))
            using (var background = new SolidBrush(p.SurfaceAlt))
                e.Graphics.FillPath(background, path);

            int progressWidth = (int)Math.Round((Width - 1) * value / 100.0);
            if (progressWidth > 1)
            {
                Rectangle progress = new Rectangle(0, 0, progressWidth, Height - 1);
                using (GraphicsPath path = UiShape.Rounded(progress, Height / 2))
                using (var foreground = new SolidBrush(p.Accent))
                    e.Graphics.FillPath(foreground, path);
            }
        }
    }

    internal static class ThemeApplier
    {
        public static void Apply(Control root, ThemePalette palette)
        {
            root.BackColor = palette.Window;
            ApplyChildren(root, palette);
        }

        private static void ApplyChildren(Control parent, ThemePalette palette)
        {
            foreach (Control control in parent.Controls)
            {
                var card = control as CardPanel;
                var button = control as ModernButton;
                var combo = control as ModernComboBox;
                var progress = control as FlatProgressBar;
                if (card != null)
                {
                    card.BackColor = palette.Surface;
                    card.BorderColor = palette.Border;
                }
                else if (button != null)
                {
                    button.Palette = palette;
                    button.Invalidate();
                }
                else if (combo != null)
                {
                    combo.Palette = palette;
                    combo.BackColor = palette.SurfaceAlt;
                    combo.ForeColor = palette.Text;
                    combo.Invalidate();
                }
                else if (progress != null)
                {
                    progress.Palette = palette;
                    progress.Invalidate();
                }
                else if (control is Label || control is CheckBox || control is RadioButton)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = palette.Text;
                }
                else if (control is ComboBox || control is NumericUpDown || control is DateTimePicker || control is ListView)
                {
                    control.BackColor = palette.SurfaceAlt;
                    control.ForeColor = palette.Text;
                }
                else if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel)
                {
                    control.BackColor = parent is CardPanel ? palette.Surface : parent.BackColor;
                }
                else if (!(control is PictureBox))
                {
                    control.BackColor = parent.BackColor;
                }

                ApplyChildren(control, palette);
            }
        }
    }
}
