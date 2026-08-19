using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class ThemePalette
    {
        public bool IsDark;
        public Color WindowTop, WindowBottom, Surface, SurfaceAlt;
        public Color GlassTop, GlassBottom, GlassBorder, Field, Border;
        public Color Text, Muted, Accent, AccentHover, AccentSoft, Danger, Success;
        public Color GlowBlue, GlowPurple;
        public Color Window { get { return WindowBottom; } }

        public static ThemePalette Create(bool dark)
        {
            if (dark)
            {
                return new ThemePalette
                {
                    IsDark = true,
                    WindowTop = Color.FromArgb(8, 13, 27), WindowBottom = Color.FromArgb(18, 19, 38),
                    Surface = Color.FromArgb(31, 39, 60), SurfaceAlt = Color.FromArgb(40, 50, 75),
                    GlassTop = Color.FromArgb(224, 31, 40, 63), GlassBottom = Color.FromArgb(206, 23, 30, 49),
                    GlassBorder = Color.FromArgb(72, 255, 255, 255), Field = Color.FromArgb(44, 55, 82),
                    Border = Color.FromArgb(82, 98, 129), Text = Color.FromArgb(247, 249, 255),
                    Muted = Color.FromArgb(173, 185, 210), Accent = Color.FromArgb(104, 151, 255),
                    AccentHover = Color.FromArgb(126, 170, 255), AccentSoft = Color.FromArgb(55, 78, 133),
                    Danger = Color.FromArgb(255, 126, 137), Success = Color.FromArgb(84, 216, 169),
                    GlowBlue = Color.FromArgb(76, 70, 130, 255), GlowPurple = Color.FromArgb(64, 172, 94, 255)
                };
            }
            return new ThemePalette
            {
                IsDark = false,
                WindowTop = Color.FromArgb(235, 243, 255), WindowBottom = Color.FromArgb(249, 246, 255),
                Surface = Color.FromArgb(246, 250, 255), SurfaceAlt = Color.FromArgb(239, 245, 255),
                GlassTop = Color.FromArgb(224, 255, 255, 255), GlassBottom = Color.FromArgb(196, 248, 251, 255),
                GlassBorder = Color.FromArgb(190, 255, 255, 255), Field = Color.FromArgb(247, 250, 255),
                Border = Color.FromArgb(190, 204, 228), Text = Color.FromArgb(24, 34, 56),
                Muted = Color.FromArgb(91, 108, 140), Accent = Color.FromArgb(67, 111, 238),
                AccentHover = Color.FromArgb(53, 93, 218), AccentSoft = Color.FromArgb(220, 230, 255),
                Danger = Color.FromArgb(221, 72, 91), Success = Color.FromArgb(17, 148, 112),
                GlowBlue = Color.FromArgb(88, 77, 147, 255), GlowPurple = Color.FromArgb(72, 189, 139, 255)
            };
        }
    }

    internal static class UiFonts
    {
        public static Font Create(float size, FontStyle style)
        {
            string preferred = L.Language == AppLanguage.Chinese ? "Microsoft YaHei UI" : "Segoe UI Variable Text";
            Font font = new Font(preferred, size, style, GraphicsUnit.Point);
            if (font.Name != preferred && L.Language != AppLanguage.Chinese)
            {
                font.Dispose();
                font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            }
            return font;
        }
    }

    internal static class UiShape
    {
        public static GraphicsPath Rounded(Rectangle rectangle, int radius)
        {
            int diameter = Math.Max(2, Math.Min(Math.Min(rectangle.Width, rectangle.Height), radius * 2));
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal static class UiText
    {
        public static void Draw(Graphics graphics, string text, Font font, Color color, Rectangle bounds,
            ContentAlignment alignment, bool wrap, bool ellipsis)
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat(StringFormat.GenericTypographic))
            {
                format.Alignment = alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.BottomCenter
                    ? StringAlignment.Center
                    : alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.BottomRight
                        ? StringAlignment.Far : StringAlignment.Near;
                format.LineAlignment = alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight
                    ? StringAlignment.Center
                    : alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight
                        ? StringAlignment.Far : StringAlignment.Near;
                if (!wrap) format.FormatFlags |= StringFormatFlags.NoWrap;
                format.Trimming = ellipsis ? StringTrimming.EllipsisCharacter : StringTrimming.None;
                graphics.DrawString(text ?? string.Empty, font, brush, bounds, format);
            }
        }
    }

    internal sealed class GlassLabel : Label
    {
        public GlassLabel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            UseCompatibleTextRendering = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiText.Draw(e.Graphics, Text, Font, Enabled ? ForeColor : SystemColors.GrayText, ClientRectangle,
                TextAlign, !AutoEllipsis, AutoEllipsis);
        }
    }

    internal static class AtmosphereRenderer
    {
        public static void Draw(Graphics graphics, Rectangle bounds, ThemePalette palette)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var background = new LinearGradientBrush(bounds, palette.WindowTop, palette.WindowBottom, 110F))
                graphics.FillRectangle(background, bounds);
            DrawGlow(graphics, new Rectangle(-bounds.Width / 5, -bounds.Height / 3, bounds.Width * 3 / 4, bounds.Height * 3 / 4), palette.GlowBlue);
            DrawGlow(graphics, new Rectangle(bounds.Width * 2 / 3, bounds.Height / 5, bounds.Width * 2 / 3, bounds.Height * 4 / 5), palette.GlowPurple);
            DrawGlow(graphics, new Rectangle(bounds.Width / 4, bounds.Height * 3 / 4, bounds.Width / 2, bounds.Height / 2), Color.FromArgb(palette.IsDark ? 36 : 44, 126, 195, 255));
        }

        private static void DrawGlow(Graphics graphics, Rectangle rectangle, Color color)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(rectangle);
                using (var glow = new PathGradientBrush(path))
                {
                    glow.CenterColor = color;
                    glow.SurroundColors = new[] { Color.FromArgb(0, color.R, color.G, color.B) };
                    glow.FocusScales = new PointF(0.08F, 0.08F);
                    graphics.FillPath(glow, path);
                }
            }
        }
    }

    internal sealed class CardPanel : Panel
    {
        public ThemePalette Palette { get; set; }
        public int Radius { get; set; }

        public CardPanel()
        {
            DoubleBuffered = true;
            Radius = 22;
            Padding = new Padding(26);
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle shadowRect = new Rectangle(3, 5, Math.Max(1, Width - 7), Math.Max(1, Height - 8));
            Rectangle cardRect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 4));
            using (GraphicsPath shadowPath = UiShape.Rounded(shadowRect, Radius))
            using (var shadow = new SolidBrush(Color.FromArgb(p.IsDark ? 46 : 18, 7, 15, 40)))
                e.Graphics.FillPath(shadow, shadowPath);
            using (GraphicsPath cardPath = UiShape.Rounded(cardRect, Radius))
            using (var fill = new LinearGradientBrush(cardRect, p.GlassTop, p.GlassBottom, 90F))
            using (var border = new Pen(p.GlassBorder, 1F))
            {
                e.Graphics.FillPath(fill, cardPath);
                e.Graphics.DrawPath(border, cardPath);
            }
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
            Height = 46;
            Font = UiFonts.Create(10F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            Color background, foreground;
            if (!Enabled) { background = p.SurfaceAlt; foreground = p.Muted; }
            else if (Primary) { background = hovered ? p.AccentHover : p.Accent; foreground = Color.White; }
            else if (Danger) { background = hovered ? Color.FromArgb(p.IsDark ? 72 : 38, p.Danger) : p.SurfaceAlt; foreground = p.Danger; }
            else { background = hovered ? p.AccentSoft : p.SurfaceAlt; foreground = p.Text; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = UiShape.Rounded(bounds, 13))
            {
                if (Primary)
                {
                    Color second = Color.FromArgb(background.A, Math.Max(0, background.R - 13), Math.Min(255, background.G + 5), Math.Min(255, background.B + 10));
                    using (var brush = new LinearGradientBrush(bounds, background, second, 0F)) e.Graphics.FillPath(brush, path);
                }
                else using (var brush = new SolidBrush(background)) e.Graphics.FillPath(brush, path);
                if (!Primary) using (var pen = new Pen(Color.FromArgb(p.IsDark ? 44 : 96, p.Border))) e.Graphics.DrawPath(pen, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class ModernComboBox : ComboBox
    {
        private const int WmPaint = 0x000F;
        public ThemePalette Palette { get; set; }

        public ModernComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            ItemHeight = 31;
            Font = UiFonts.Create(10F, FontStyle.Regular);
            IntegralHeight = false;
            DropDownHeight = 240;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            int index = e.Index >= 0 ? e.Index : SelectedIndex;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var brush = new SolidBrush(selected ? p.AccentSoft : p.Field)) e.Graphics.FillRectangle(brush, e.Bounds);
            if (index >= 0 && index < Items.Count)
            {
                Rectangle textBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 18), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, GetItemText(Items[index]), Font, textBounds, Enabled ? p.Text : p.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WmPaint && DropDownStyle == ComboBoxStyle.DropDownList && IsHandleCreated)
                using (Graphics graphics = Graphics.FromHwnd(Handle)) DrawClosedState(graphics);
        }

        private void DrawClosedState(Graphics graphics)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var outside = new SolidBrush(p.Surface)) graphics.FillRectangle(outside, ClientRectangle);
            using (GraphicsPath path = UiShape.Rounded(bounds, 10))
            using (var fill = new SolidBrush(p.Field))
            using (var border = new Pen(p.Border))
            { graphics.FillPath(fill, path); graphics.DrawPath(border, path); }
            string value = SelectedIndex >= 0 ? GetItemText(SelectedItem) : string.Empty;
            Rectangle textBounds = new Rectangle(13, 1, Math.Max(1, Width - 50), Math.Max(1, Height - 2));
            TextRenderer.DrawText(graphics, value, Font, textBounds, Enabled ? p.Text : p.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            int centerX = Width - 20, centerY = Height / 2;
            using (var pen = new Pen(Enabled ? p.Muted : p.Border, 1.6F))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, centerX - 4, centerY - 2, centerX, centerY + 2);
                graphics.DrawLine(pen, centerX, centerY + 2, centerX + 4, centerY - 2);
            }
        }
    }

    internal sealed class GlassNumberInput : UserControl
    {
        private readonly TextBox editor = new TextBox();
        private decimal minimum;
        private decimal maximum = 100;
        private decimal number;
        private bool updating;
        public ThemePalette Palette { get; set; }
        public decimal Minimum { get { return minimum; } set { minimum = value; Value = number; } }
        public decimal Maximum { get { return maximum; } set { maximum = value; Value = number; } }
        public decimal Value
        {
            get { CommitEditor(); return number; }
            set
            {
                decimal normalized = Math.Max(minimum, Math.Min(maximum, value));
                number = normalized;
                updating = true;
                editor.Text = normalized.ToString("0");
                updating = false;
                Invalidate();
            }
        }

        public GlassNumberInput()
        {
            DoubleBuffered = true;
            Height = 40;
            MinimumSize = new Size(64, 40);
            Font = UiFonts.Create(10F, FontStyle.Regular);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            editor.BorderStyle = BorderStyle.None;
            editor.AutoSize = false;
            editor.TextAlign = HorizontalAlignment.Center;
            editor.Text = "0";
            editor.MaxLength = 3;
            editor.KeyPress += EditorKeyPress;
            editor.LostFocus += delegate { CommitEditor(); };
            Controls.Add(editor);
        }

        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (editor != null) editor.Font = Font; LayoutEditor(); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutEditor(); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); editor.ReadOnly = !Enabled; UpdateColors(); Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e) { if (Enabled) Step(e.Delta > 0 ? 1 : -1); base.OnMouseWheel(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.X >= Width - 34) Step(e.Y < Height / 2 ? 1 : -1); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            DrawField(e.Graphics, ClientRectangle, p, 34);
            DrawChevron(e.Graphics, Width - 17, 12, true, Enabled ? p.Muted : p.Border);
            DrawChevron(e.Graphics, Width - 17, Height - 12, false, Enabled ? p.Muted : p.Border);
        }

        internal static void DrawField(Graphics graphics, Rectangle client, ThemePalette palette, int stepperWidth)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, client.Width - 1), Math.Max(1, client.Height - 1));
            using (GraphicsPath path = UiShape.Rounded(bounds, 10))
            using (var fill = new SolidBrush(palette.Field))
            using (var border = new Pen(palette.Border))
            { graphics.FillPath(fill, path); graphics.DrawPath(border, path); }
            using (var divider = new Pen(Color.FromArgb(palette.IsDark ? 76 : 115, palette.Border)))
                graphics.DrawLine(divider, client.Width - stepperWidth, 7, client.Width - stepperWidth, client.Height - 8);
        }

        internal static void DrawChevron(Graphics graphics, int x, int y, bool up, Color color)
        {
            int direction = up ? -1 : 1;
            using (var pen = new Pen(color, 1.4F))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, x - 3, y + direction, x, y - direction * 2);
                graphics.DrawLine(pen, x, y - direction * 2, x + 3, y + direction);
            }
        }

        private void LayoutEditor()
        {
            if (editor == null) return;
            int editorHeight = TextRenderer.MeasureText("00", Font).Height + 2;
            editor.SetBounds(8, Math.Max(2, (Height - editorHeight) / 2), Math.Max(10, Width - 50), editorHeight);
            UpdateColors();
        }

        private void UpdateColors()
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            editor.BackColor = p.Field;
            editor.ForeColor = Enabled ? p.Text : p.Muted;
        }

        private void Step(int delta) { CommitEditor(); Value = number + delta; }
        private void CommitEditor()
        {
            if (updating) return;
            decimal parsed;
            if (!decimal.TryParse(editor.Text, out parsed)) parsed = number;
            Value = parsed;
        }

        private void EditorKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            if (e.KeyChar == (char)Keys.Enter) { CommitEditor(); e.Handled = true; }
        }
    }

    internal sealed class GlassTimeInput : UserControl
    {
        private readonly TextBox editor = new TextBox();
        private DateTime time = DateTime.Today;
        private bool updating;
        public ThemePalette Palette { get; set; }
        public DateTime Value
        {
            get { CommitEditor(); return time; }
            set
            {
                time = DateTime.Today.Add(value.TimeOfDay);
                updating = true; editor.Text = time.ToString("HH:mm:ss"); updating = false;
                Invalidate();
            }
        }

        public GlassTimeInput()
        {
            DoubleBuffered = true;
            Height = 40;
            Font = UiFonts.Create(10F, FontStyle.Regular);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            editor.BorderStyle = BorderStyle.None;
            editor.AutoSize = false;
            editor.TextAlign = HorizontalAlignment.Left;
            editor.Text = time.ToString("HH:mm:ss");
            editor.MaxLength = 8;
            editor.LostFocus += delegate { CommitEditor(); };
            editor.KeyDown += EditorKeyDown;
            Controls.Add(editor);
        }

        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (editor != null) editor.Font = Font; LayoutEditor(); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutEditor(); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); editor.ReadOnly = !Enabled; UpdateColors(); Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e) { if (Enabled) StepMinutes(e.Delta > 0 ? 1 : -1); base.OnMouseWheel(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.X >= Width - 42) StepMinutes(e.Y < Height / 2 ? 1 : -1); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            GlassNumberInput.DrawField(e.Graphics, ClientRectangle, p, 42);
            GlassNumberInput.DrawChevron(e.Graphics, Width - 21, 12, true, Enabled ? p.Muted : p.Border);
            GlassNumberInput.DrawChevron(e.Graphics, Width - 21, Height - 12, false, Enabled ? p.Muted : p.Border);
        }

        private void LayoutEditor()
        {
            if (editor == null) return;
            int editorHeight = TextRenderer.MeasureText("00:00:00", Font).Height + 2;
            editor.SetBounds(14, Math.Max(2, (Height - editorHeight) / 2), Math.Max(20, Width - 66), editorHeight);
            UpdateColors();
        }

        private void UpdateColors()
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            editor.BackColor = p.Field;
            editor.ForeColor = Enabled ? p.Text : p.Muted;
        }

        private void StepMinutes(int minutes) { CommitEditor(); Value = time.AddMinutes(minutes); }
        private void CommitEditor()
        {
            if (updating) return;
            TimeSpan parsed;
            if (TimeSpan.TryParse(editor.Text, out parsed) && parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1)) Value = DateTime.Today.Add(parsed);
            else Value = time;
        }

        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            { StepMinutes(e.KeyCode == Keys.Up ? 1 : -1); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter) { CommitEditor(); e.SuppressKeyPress = true; }
        }
    }

    internal class GlassCheckBox : CheckBox
    {
        private bool hovered;
        public ThemePalette Palette { get; set; }

        public GlassCheckBox()
        {
            Height = 32;
            Cursor = Cursors.Hand;
            Font = UiFonts.Create(9.6F, FontStyle.Regular);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(1, (Height - 19) / 2, 19, 19);
            using (GraphicsPath path = UiShape.Rounded(box, 5))
            using (var fill = new SolidBrush(Checked ? p.Accent : (hovered ? p.AccentSoft : p.Field)))
            using (var border = new Pen(Checked ? p.Accent : p.Border))
            { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path); }
            if (Checked)
            {
                using (var pen = new Pen(Color.White, 2F))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(pen, new[] { new Point(5, box.Top + 10), new Point(9, box.Top + 14), new Point(16, box.Top + 6) });
                }
            }
            Rectangle textBounds = new Rectangle(29, 0, Math.Max(1, Width - 30), Height);
            UiText.Draw(e.Graphics, Text, Font, Enabled ? p.Text : p.Muted, textBounds,
                ContentAlignment.MiddleLeft, false, true);
        }
    }

    internal sealed class GlassRadioButton : RadioButton
    {
        private bool hovered;
        public ThemePalette Palette { get; set; }

        public GlassRadioButton()
        {
            Height = 32;
            Cursor = Cursors.Hand;
            Font = UiFonts.Create(9.6F, FontStyle.Regular);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle circle = new Rectangle(1, (Height - 19) / 2, 19, 19);
            using (var fill = new SolidBrush(hovered ? p.AccentSoft : p.Field)) e.Graphics.FillEllipse(fill, circle);
            using (var border = new Pen(Checked ? p.Accent : p.Border, Checked ? 2F : 1F)) e.Graphics.DrawEllipse(border, circle);
            if (Checked)
                using (var dot = new SolidBrush(p.Accent)) e.Graphics.FillEllipse(dot, new Rectangle(circle.X + 5, circle.Y + 5, 9, 9));
            Rectangle textBounds = new Rectangle(29, 0, Math.Max(1, Width - 30), Height);
            UiText.Draw(e.Graphics, Text, Font, Enabled ? p.Text : p.Muted, textBounds,
                ContentAlignment.MiddleLeft, false, true);
        }
    }

    internal sealed class FlatProgressBar : Control
    {
        private int amount;
        public ThemePalette Palette { get; set; }
        public int Value { get { return amount; } set { amount = Math.Max(0, Math.Min(100, value)); Invalidate(); } }

        public FlatProgressBar()
        {
            Height = 8;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette p = Palette ?? ThemePalette.Create(false);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle full = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = UiShape.Rounded(full, Height / 2))
            using (var background = new SolidBrush(p.SurfaceAlt)) e.Graphics.FillPath(background, path);
            int progressWidth = (int)Math.Round((Width - 1) * amount / 100.0);
            if (progressWidth > 1)
            {
                Rectangle progress = new Rectangle(0, 0, progressWidth, Height - 1);
                using (GraphicsPath path = UiShape.Rounded(progress, Height / 2))
                using (var foreground = new LinearGradientBrush(progress, p.Accent, p.AccentHover, 0F)) e.Graphics.FillPath(foreground, path);
            }
        }
    }

    internal class FrostedForm : Form
    {
        public ThemePalette GlassPalette { get; set; }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            AtmosphereRenderer.Draw(e.Graphics, ClientRectangle, GlassPalette ?? ThemePalette.Create(false));
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
                CardPanel card = control as CardPanel;
                ModernButton button = control as ModernButton;
                ModernComboBox combo = control as ModernComboBox;
                GlassNumberInput number = control as GlassNumberInput;
                GlassTimeInput time = control as GlassTimeInput;
                GlassCheckBox check = control as GlassCheckBox;
                GlassRadioButton radio = control as GlassRadioButton;
                FlatProgressBar progress = control as FlatProgressBar;
                if (card != null) { card.Palette = palette; card.BackColor = Color.Transparent; card.Invalidate(); }
                else if (button != null) { button.Palette = palette; button.Invalidate(); }
                else if (combo != null) { combo.Palette = palette; combo.BackColor = palette.Field; combo.ForeColor = palette.Text; combo.Invalidate(); }
                else if (number != null) { number.Palette = palette; number.Invalidate(true); }
                else if (time != null) { time.Palette = palette; time.Invalidate(true); }
                else if (check != null) { check.Palette = palette; check.Invalidate(); }
                else if (radio != null) { radio.Palette = palette; radio.Invalidate(); }
                else if (progress != null) { progress.Palette = palette; progress.Invalidate(); }
                else if (control is Label) { control.BackColor = Color.Transparent; control.ForeColor = palette.Text; }
                else if (control is ListView) { control.BackColor = palette.Field; control.ForeColor = palette.Text; }
                else if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel) control.BackColor = Color.Transparent;
                else if (!(control is PictureBox)) { control.BackColor = palette.Field; control.ForeColor = palette.Text; }
                ApplyChildren(control, palette);
            }
        }
    }
}
