using System.Data;

namespace Valcongrup.UserControls;

internal class RoundedPanel : Panel
{
    public int Radius { get; set; } = 18;
    public Color BorderColor { get; set; } = AppTheme.Border;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.Card;
        Padding = new Padding(22);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width > 0 && Height > 0)
            Region = new Region(AppTheme.RoundedPath(new Rectangle(0, 0, Width, Height), Radius));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        AppTheme.ApplyHighQualityGraphics(e.Graphics);
        using var pen = new Pen(BorderColor, 1);
        e.Graphics.DrawPath(pen, AppTheme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius));
    }
}

internal static class ModernUi
{
    public static Label Title(string text) => new() { Text = text, ForeColor = AppTheme.TextOnLight, Font = AppTheme.Font(24f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
    public static Label Subtitle(string text) => new() { Text = text, ForeColor = AppTheme.TextOnLightSecondary, Font = AppTheme.Font(10.5f), AutoSize = true, BackColor = Color.Transparent };
    public static Label Text(string text, float size = 10f, Color? color = null, FontStyle style = FontStyle.Regular) => new() { Text = text, ForeColor = color ?? AppTheme.TextOnLight, Font = AppTheme.Font(size, style), AutoSize = true, BackColor = Color.Transparent };

    public static Panel PageHeader(string title, string subtitle, params Control[] actions)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 136, BackColor = AppTheme.Shell, Padding = new Padding(48, 28, 48, 12) };
        var t = Title(title);
        var s = Subtitle(subtitle);
        t.Location = new Point(0, 18);
        s.Location = new Point(0, 68);
        p.Controls.AddRange(new Control[] { t, s });
        p.Resize += (_, _) => PlaceActions(p, actions);
        PlaceActions(p, actions);
        return p;
    }

    private static void PlaceActions(Control host, IReadOnlyList<Control> actions)
    {
        var right = host.Width - 48;
        foreach (var a in actions.Reverse())
        {
            a.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            right -= a.Width;
            var centeredY = (host.Height - a.Height) / 2;
            a.Location = new Point(right, centeredY);
            right -= 16;
            if (!host.Controls.Contains(a)) host.Controls.Add(a);
        }
    }

    public static Panel SearchBox(string placeholder, int width = 520)
    {
        var p = new RoundedPanel { Width = width, Height = 52, Radius = 16, BackColor = Color.White, Padding = new Padding(54, 12, 18, 8), BorderColor = AppTheme.Border };
        p.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            using var pen = new Pen(AppTheme.TextOnLightSecondary, 1.8f);
            e.Graphics.DrawEllipse(pen, 20, 17, 14, 14);
            e.Graphics.DrawLine(pen, 31, 29, 38, 36);
        };
        var label = Text(placeholder, 11f, Color.FromArgb(100, 116, 139));
        label.Dock = DockStyle.Fill;
        p.Controls.Add(label);
        return p;
    }

    public static Button GhostButton(string text, int width = 140)
    {
        var b = new Button { Text = text, Width = width, Height = 46, BackColor = Color.White, ForeColor = AppTheme.TextOnLight, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = AppTheme.Font(10f, FontStyle.Bold), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderColor = AppTheme.ShellBorder;
        b.FlatAppearance.BorderSize = 1;
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.CardAlt;
        b.MouseLeave += (_, _) => b.BackColor = Color.White;
        AppTheme.ApplyRoundedRegion(b, 14);
        b.Resize += (_, _) => AppTheme.ApplyRoundedRegion(b, 14);
        return b;
    }

    public static DataGridView Grid()
    {
        var dgv = new DataGridView { Dock = DockStyle.Fill };
        AppTheme.StyleModernGrid(dgv);
        dgv.Font = AppTheme.Font(10f);
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 14, 0);
        dgv.DefaultCellStyle.Padding              = new Padding(14, 0, 14, 0);
        dgv.AllowUserToOrderColumns               = false;
        dgv.AllowUserToResizeRows                 = false;
        dgv.AllowUserToResizeColumns              = false;
        dgv.ColumnHeadersHeightSizeMode           = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgv.RowTemplate.Height                    = 46;
        return dgv;
    }

    public static void PaintStatusBadge(DataGridView grid, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Status") return;

        e.Handled = true;
        e.PaintBackground(e.ClipBounds, true);

        var status = e.Value?.ToString() ?? "";
        var (bg, fg) = status switch
        {
            "Activ" => (Color.FromArgb(209, 250, 229), AppTheme.Success),
            "Planificat" => (Color.FromArgb(254, 243, 199), AppTheme.Warning),
            "Finalizat" => (Color.FromArgb(243, 244, 246), AppTheme.TextSecond),
            "Suspendat" => (Color.FromArgb(254, 226, 226), AppTheme.Danger),
            "Cerere" => (Color.FromArgb(255, 237, 213), AppTheme.Accent),
            _ => (Color.FromArgb(243, 244, 246), AppTheme.TextSecond)
        };

        using var f = AppTheme.Font(8.5f, FontStyle.Bold);
        var textSize = TextRenderer.MeasureText(status, f);
        var width = Math.Min(e.CellBounds.Width - 24, textSize.Width + 24);
        var badge = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y + (e.CellBounds.Height - 28) / 2, Math.Max(40, width), 28);
        var g = e.Graphics;
        if (g == null) return;
        AppTheme.ApplyHighQualityGraphics(g);
        using (var path = AppTheme.RoundedPath(badge, 14))
        using (var brush = new SolidBrush(bg))
            g.FillPath(brush, path);
        TextRenderer.DrawText(g, status, f, badge, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    public static DataTable ToProjectTable(IEnumerable<Models.Proiect> projects)
    {
        var dt = new DataTable();
        dt.Columns.Add("Nume Proiect");
        dt.Columns.Add("Client");
        dt.Columns.Add("Buget Total");
        dt.Columns.Add("Procent Finalizat");
        dt.Columns.Add("Data Start");
        dt.Columns.Add("Data Limită");
        dt.Columns.Add("Status");
        foreach (var p in projects)
            dt.Rows.Add(p.Nume, p.NumeClient, $"{p.BugetTotal:N0} MDL", $"{p.Progres}%", p.DataStart.ToString("dd MMM yyyy"), p.DataTermen.ToString("dd MMM yyyy"), p.Status);
        return dt;
    }
}
