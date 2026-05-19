using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Valcongrup.UserControls;

namespace Valcongrup;

public static class AppTheme
{
    public static bool IsDarkMode { get; private set; }
    public static event EventHandler? ThemeChanged;

    public static Color BgDark = Color.FromArgb(255, 255, 255);
    public static Color BgPanel = Color.FromArgb(255, 255, 255);
    public static Color BgCard = Color.FromArgb(255, 250, 245);
    public static Color Accent = Color.FromArgb(249, 115, 22);
    public static Color AccentHover = Color.FromArgb(234, 88, 12);
    public static Color Success = Color.FromArgb(16, 185, 129);
    public static Color Warning = Color.FromArgb(245, 158, 11);
    public static Color Danger = Color.FromArgb(239, 68, 68);
    public static Color TextPrimary = Color.FromArgb(15, 23, 42);
    public static Color TextSecond = Color.FromArgb(100, 116, 139);
    public static Color Border = Color.FromArgb(226, 232, 240);
    public static Color SidebarBg = Color.FromArgb(255, 255, 255);
    public static Color SidebarHover = Color.FromArgb(255, 250, 245);
    public static Color SidebarActive = Color.FromArgb(249, 115, 22);
    public static Color Blue = Color.FromArgb(249, 115, 22);
    /// <summary>Fundal principal aplicație (tema deschisă — conținut + bară sus).</summary>
    public static Color Shell = Color.FromArgb(255, 255, 255);
    /// <summary>Sidebar și panouri secundare — alb (tema unificată).</summary>
    public static Color Shell2 = Color.FromArgb(255, 255, 255);
    /// <summary>Borduri pentru shell light (topbar / sidebar).</summary>
    public static Color ShellBorder = Color.FromArgb(226, 232, 240);
    public static Color Card = Color.FromArgb(255, 255, 255);
    public static Color CardAlt = Color.FromArgb(255, 250, 245);
    public static Color MutedBlue = Color.FromArgb(120, 132, 150);
    /// <summary>Text principal pe fundal deschis.</summary>
    public static Color TextOnLight = Color.FromArgb(15, 23, 42);
    /// <summary>Text secundar pe fundal deschis.</summary>
    public static Color TextOnLightSecondary = Color.FromArgb(100, 116, 139);
    public static Color TextOnDark = Color.FromArgb(15, 23, 42);
    public static Color TextOnDarkSecondary = Color.FromArgb(120, 132, 150);
    public static Color LoginRed = Color.FromArgb(255, 34, 48);
    public static Color LoginRedHover = Color.FromArgb(234, 23, 38);

    public static Font Font(float size = 8.5f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    public static void SetMode(bool dark)
    {
        IsDarkMode = dark;
        if (dark)
        {
            BgDark = Color.FromArgb(15, 23, 42);
            BgPanel = Color.FromArgb(17, 24, 39);
            BgCard = Color.FromArgb(31, 41, 55);
            TextPrimary = Color.FromArgb(248, 250, 252);
            TextSecond = Color.FromArgb(203, 213, 225);
            Border = Color.FromArgb(51, 65, 85);
            SidebarBg = Color.FromArgb(15, 23, 42);
            SidebarHover = Color.FromArgb(30, 41, 59);
            Shell = Color.FromArgb(15, 23, 42);
            Shell2 = Color.FromArgb(17, 24, 39);
            ShellBorder = Color.FromArgb(51, 65, 85);
            Card = Color.FromArgb(17, 24, 39);
            CardAlt = Color.FromArgb(30, 41, 59);
            TextOnLight = TextPrimary;
            TextOnLightSecondary = TextSecond;
            TextOnDark = TextPrimary;
            TextOnDarkSecondary = TextSecond;
        }
        else
        {
            BgDark = Color.FromArgb(255, 255, 255);
            BgPanel = Color.FromArgb(255, 255, 255);
            BgCard = Color.FromArgb(255, 250, 245);
            TextPrimary = Color.FromArgb(15, 23, 42);
            TextSecond = Color.FromArgb(100, 116, 139);
            Border = Color.FromArgb(226, 232, 240);
            SidebarBg = Color.FromArgb(255, 255, 255);
            SidebarHover = Color.FromArgb(255, 250, 245);
            Shell = Color.FromArgb(255, 255, 255);
            Shell2 = Color.FromArgb(255, 255, 255);
            ShellBorder = Color.FromArgb(226, 232, 240);
            Card = Color.FromArgb(255, 255, 255);
            CardAlt = Color.FromArgb(255, 250, 245);
            TextOnLight = Color.FromArgb(15, 23, 42);
            TextOnLightSecondary = Color.FromArgb(100, 116, 139);
            TextOnDark = Color.FromArgb(15, 23, 42);
            TextOnDarkSecondary = Color.FromArgb(120, 132, 150);
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ApplyToTree(Control root)
    {
        root.BackColor = root switch
        {
            Form => Shell,
            DataGridView => BgPanel,
            System.Windows.Forms.TextBox => IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.White,
            System.Windows.Forms.ComboBox => IsDarkMode ? Color.FromArgb(30, 41, 59) : CardAlt,
            Button => root.BackColor == Accent || root.BackColor == AccentHover ? Accent : Card,
            RoundedPanel => Card,
            Panel when IsDarkMode && root.BackColor != Color.Transparent => Card,
            Panel when !IsDarkMode && root.BackColor != Color.Transparent => Color.White,
            _ when root.BackColor != Color.Transparent => root.BackColor,
            _ => root.BackColor
        };

        root.ForeColor = root switch
        {
            Label lbl when lbl.ForeColor == Danger || lbl.ForeColor == Success => lbl.ForeColor,
            Button btn when btn.BackColor == Accent || btn.BackColor == AccentHover => Color.White,
            Button => TextPrimary,
            System.Windows.Forms.TextBox or System.Windows.Forms.ComboBox or Label => TextPrimary,
            _ => root.ForeColor
        };

        if (root is DataGridView dgv)
            StyleGrid(dgv);

        foreach (Control child in root.Controls)
            ApplyToTree(child);

        root.Invalidate();
    }

    public static void ApplyRoundedRegion(Control control, int radius = -1)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        var actualRadius = radius < 0 ? Math.Max(8, control.Height / 2) : radius;
        control.Region = new Region(RoundedPath(new Rectangle(0, 0, control.Width, control.Height), actualRadius));
    }

    public static Button PrimaryButton(string text, int width = 140, int height = 38)
    {
        var b = new Button { Text = text, Width = width, Height = height, BackColor = Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = Font(9.5f, FontStyle.Bold), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => b.BackColor = AccentHover;
        b.MouseLeave += (_, _) => b.BackColor = Accent;
        ApplyRoundedRegion(b);
        b.Resize += (_, _) => ApplyRoundedRegion(b);
        return b;
    }

    public static Button SecondaryButton(string text, int width = 120, int height = 36)
    {
        var b = new Button { Text = text, Width = width, Height = height, BackColor = Card, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = Font(9f, FontStyle.Bold), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderColor = Border;
        b.FlatAppearance.BorderSize = 1;
        b.MouseEnter += (_, _) => b.BackColor = CardAlt;
        b.MouseLeave += (_, _) => b.BackColor = Card;
        ApplyRoundedRegion(b);
        b.Resize += (_, _) => ApplyRoundedRegion(b);
        return b;
    }

    public static void StyleGrid(DataGridView dgv)
    {
        dgv.BackgroundColor = BgPanel;
        dgv.GridColor = Border;
        dgv.DefaultCellStyle.BackColor = BgPanel;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(255, 247, 237);
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = CardAlt;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Font = Font(8.5f, FontStyle.Bold);
        dgv.AlternatingRowsDefaultCellStyle.BackColor = CardAlt;
        dgv.EnableHeadersVisualStyles = false;
        dgv.BorderStyle = BorderStyle.None;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToOrderColumns = false;
        dgv.AllowUserToResizeRows = false;
        dgv.AllowUserToResizeColumns = false;
        dgv.ReadOnly = true;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.RowTemplate.Height = 42;
        dgv.Font = Font(10f);
        dgv.DefaultCellStyle.Font = Font(10f);
        dgv.ColumnHeadersDefaultCellStyle.Font = Font(9.5f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = CardAlt;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
    }

    public static TextBox TextBox() => new() { BackColor = Color.White, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, Font = Font(9.5f) };
    public static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardAlt, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = Font(9.5f) };

    /// <summary>Buton accent ca pe login (portocaliu). Fără Region — evită margini zimțate și click-uri pierdute.</summary>
    public static Button AccentButton(string text, int width = 156, int height = 54)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = Font(10.5f, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => b.BackColor = AccentHover;
        b.MouseLeave += (_, _) => b.BackColor = Accent;
        ApplyRoundedRegion(b, Math.Max(12, height / 2));
        b.Resize += (_, _) => ApplyRoundedRegion(b, Math.Max(12, b.Height / 2));
        return b;
    }

    public static Button BlueButton(string text, int width = 190, int height = 54)
    {
        var b = new Button { Text = text, Width = width, Height = height, BackColor = Blue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = Font(10.5f, FontStyle.Bold), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0;
        ApplyRoundedRegion(b, Math.Max(12, height / 2));
        b.Resize += (_, _) => ApplyRoundedRegion(b, Math.Max(12, b.Height / 2));
        return b;
    }

    public static void StyleModernGrid(DataGridView dgv)
    {
        StyleGrid(dgv);
        dgv.BackgroundColor = Card;
        dgv.DefaultCellStyle.BackColor = Card;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(255, 247, 237);
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = CardAlt;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersHeight = 50;
        dgv.RowTemplate.Height = 54;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = Border;
        dgv.Dock = DockStyle.Fill;
    }

    public static void ApplyHighQualityGraphics(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static void DrawImageCover(Graphics g, Image image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || image.Width <= 0 || image.Height <= 0)
            return;

        var scale = Math.Max(bounds.Width / (float)image.Width, bounds.Height / (float)image.Height);
        var width = (int)Math.Ceiling(image.Width * scale);
        var height = (int)Math.Ceiling(image.Height * scale);
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        g.DrawImage(image, new Rectangle(x, y, width, height));
    }

    public static async void MarkInvalid(Control control)
    {
        var originalBack = control.BackColor;
        var originalLocation = control.Location;
        control.BackColor = Color.FromArgb(255, 235, 238);
        control.Refresh();

        var offsets = new[] { -7, 7, -5, 5, -2, 2, 0 };
        foreach (var offset in offsets)
        {
            if (control.IsDisposed) return;
            control.Location = new Point(originalLocation.X + offset, originalLocation.Y);
            await Task.Delay(28);
        }

        if (control.IsDisposed) return;
        control.Location = originalLocation;
        await Task.Delay(550);
        if (!control.IsDisposed) control.BackColor = originalBack;
    }

    public static async void FadeIn(Form form, double step = 0.18)
    {
        if (form.IsDisposed) return;
        form.Opacity = 0;
        while (!form.IsDisposed && form.Opacity < 1)
        {
            form.Opacity = Math.Min(1, form.Opacity + step);
            await Task.Delay(8);
        }
    }

    public static async Task FadeOut(Form form, double step = 0.22)
    {
        while (!form.IsDisposed && form.Opacity > 0)
        {
            form.Opacity = Math.Max(0, form.Opacity - step);
            await Task.Delay(8);
        }
    }

    public static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
