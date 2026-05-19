using System.Data;
using Valcongrup.Data;

namespace Valcongrup.UserControls;

internal static class UiFactory
{
    public static Label Title(string text) => new() { Text = text, ForeColor = AppTheme.TextPrimary, Font = AppTheme.Font(18f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
    public static Label Label(string text, float size = 9f, Color? color = null) => new() { Text = text, ForeColor = color ?? AppTheme.TextPrimary, Font = AppTheme.Font(size), AutoSize = true, BackColor = Color.Transparent };
    public static DataGridView Grid() { var dgv = new DataGridView { Dock = DockStyle.Fill }; AppTheme.StyleGrid(dgv); return dgv; }
    public static void Try(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show("Eroare: " + ex.Message, "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
    public static DataTable ProjectsLookup() => RepositoryHelpers.Fill("SELECT id,nume FROM proiecte ORDER BY nume");
    public static DataTable UsersLookup() => RepositoryHelpers.Fill("SELECT id,CONCAT(prenume,' ',nume) AS nume FROM utilizatori WHERE activ=1 ORDER BY prenume,nume");
    public static DataTable ClientsLookup() => RepositoryHelpers.Fill("SELECT id,nume FROM clienti ORDER BY nume");

    public static FlowLayoutPanel Header(string title, Button? button = null)
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(18, 14, 18, 8), BackColor = AppTheme.BgDark, FlowDirection = FlowDirection.LeftToRight };
        p.Controls.Add(Title(title));
        if (button != null)
        {
            button.Margin = new Padding(24, 0, 0, 0);
            p.Controls.Add(button);
        }
        return p;
    }

    public static Panel Field(string label, Control input)
    {
        var p = new Panel { Height = 58, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };
        var l = Label(label, 8.5f, AppTheme.TextSecond);
        l.Dock = DockStyle.Top;
        input.Dock = DockStyle.Bottom;
        input.Height = 28;
        p.Controls.Add(input);
        p.Controls.Add(l);
        return p;
    }
}
