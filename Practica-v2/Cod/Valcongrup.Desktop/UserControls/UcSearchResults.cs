using Valcongrup.Models;

namespace Valcongrup.UserControls;

/// <summary>
/// Shown in the main content area when the user executes a global search.
/// Displays matching projects in a styled, locked DataGridView.
/// </summary>
public class UcSearchResults : UserControl
{
    private readonly DataGridView _grid = ModernUi.Grid();

    public UcSearchResults(string keyword, List<Proiect> results)
    {
        BackColor = AppTheme.Shell;
        Padding   = new Padding(48, 0, 32, 32);

        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader(
            $"Rezultate căutare: \"{keyword}\"",
            $"{results.Count} proiect(e) găsite"));

        BindResults(results);
    }

    private Control BuildTable()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill, Radius = 18,
            BackColor = AppTheme.Card, Padding = new Padding(0),
            Margin = new Padding(0, 20, 0, 0)
        };

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Proiect",      DataPropertyName = "Nume",        FillWeight = 26, MinimumWidth = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Client",       DataPropertyName = "NumeClient",  FillWeight = 22, MinimumWidth = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Manager",      DataPropertyName = "NumeManager", FillWeight = 20, MinimumWidth = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Buget Total",  DataPropertyName = "BugetFmt",    FillWeight = 16, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Termen",       DataPropertyName = "TermenFmt",   FillWeight = 16, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", FillWeight = 12, MinimumWidth = 90 });

        _grid.CellPainting += (_, e) => ModernUi.PaintStatusBadge(_grid, e);

        card.Controls.Add(_grid);
        return card;
    }

    private void BindResults(List<Proiect> results)
    {
        var rows = results.Select(p => new SearchRow(p)).ToList();
        void Bind() => _grid.DataSource = rows;
        if (InvokeRequired) Invoke(Bind); else Bind();
    }

    private sealed class SearchRow
    {
        public SearchRow(Proiect p)
        {
            Nume        = p.Nume;
            NumeClient  = p.NumeClient;
            NumeManager = p.NumeManager;
            BugetFmt    = $"{p.BugetTotal:N0} MDL";
            TermenFmt   = p.DataTermen.ToString("dd MMM yyyy");
            Status      = p.Status;
        }
        public string Nume        { get; }
        public string NumeClient  { get; }
        public string NumeManager { get; }
        public string BugetFmt    { get; }
        public string TermenFmt   { get; }
        public string Status      { get; }
    }
}
