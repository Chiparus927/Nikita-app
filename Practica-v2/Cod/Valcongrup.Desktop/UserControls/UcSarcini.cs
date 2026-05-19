using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;

namespace Valcongrup.UserControls;

public class UcSarcini : UserControl
{
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly SarciniRepository _repo = new();
    private readonly ComboBox _filterType = AppTheme.Combo();
    private readonly ComboBox _filterValue = AppTheme.Combo();

    public UcSarcini()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);

        var add = AppTheme.AccentButton("+  Adaugă Sarcină", 200, 46);
        add.Click += (_, _) => Add();

        _filterType.Width = 140;
        _filterValue.Width = 150;
        _filterType.Items.AddRange(new object[] { "Toate", "Prioritate", "Status" });
        _filterType.SelectedIndex = 0;
        _filterValue.Visible = false;
        _filterType.SelectedIndexChanged += (_, _) => ConfigureFilterValues();
        _filterValue.SelectedIndexChanged += (_, _) => LoadData();

        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader("Sarcini", "Urmareste si gestioneaza sarcinile echipei tale.", _filterType, _filterValue, add));
        _grid.CellFormatting += GridFormatting;
        LoadData();
    }

    private Control BuildTable()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 18, BackColor = AppTheme.Card, Padding = new Padding(0), Margin = new Padding(0, 20, 0, 0) };
        var search = ModernUi.SearchBox("Cauta o sarcina dupa nume...", 560);
        search.Location = new Point(24, 24);
        card.Controls.Add(search);
        _grid.Location = new Point(0, 108);
        _grid.Size = new Size(card.Width, card.Height - 108);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        card.Controls.Add(_grid);
        return card;
    }

    private void LoadData()
    {
        var type = _filterType.SelectedItem?.ToString() ?? "Toate";
        var value = _filterValue.Visible ? _filterValue.SelectedItem?.ToString() : null;
        UiFactory.Try(() => _grid.DataSource = _repo.LoadTasksData(type, value));
    }

    private void ConfigureFilterValues()
    {
        var type = _filterType.SelectedItem?.ToString() ?? "Toate";
        _filterValue.Items.Clear();
        _filterValue.Visible = type != "Toate";
        if (type == "Prioritate")
            _filterValue.Items.AddRange(new object[] { "Urgenta", "Ridicata", "Medie", "Scazuta" });
        else if (type == "Status")
            _filterValue.Items.AddRange(new object[] { "In Lucru", "Planificat", "Finalizat", "In Asteptare" });
        if (_filterValue.Items.Count > 0) _filterValue.SelectedIndex = 0;
        LoadData();
    }

    private void GridFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0) return;
        var header = _grid.Columns[e.ColumnIndex].HeaderText;
        if (header == "Nume Sarcina") e.CellStyle!.Font = AppTheme.Font(10f, FontStyle.Bold);
        if (header == "Prioritate")
        {
            e.CellStyle!.ForeColor = e.Value?.ToString() switch { "Urgenta" => Color.FromArgb(239, 68, 68), "Ridicata" => Color.FromArgb(245, 158, 11), "Medie" => Color.FromArgb(59, 130, 246), "Scazuta" => Color.FromArgb(148, 163, 184), _ => AppTheme.MutedBlue };
            e.CellStyle.Font = AppTheme.Font(10f, FontStyle.Bold);
        }
        if (header == "Status")
        {
            e.CellStyle!.ForeColor = e.Value?.ToString() switch { "Finalizata" or "Finalizat" => AppTheme.Success, "In Progres" or "In Lucru" => AppTheme.Warning, "In Asteptare" => Color.FromArgb(148, 163, 184), _ => AppTheme.MutedBlue };
            e.CellStyle.Font = AppTheme.Font(10f, FontStyle.Bold);
        }
    }

    private void Add()
    {
        using var dlg = new DialogSarcina();
        if (dlg.ShowDialog() == DialogResult.OK)
            UiFactory.Try(() =>
            {
                var id = _repo.Insert(dlg.Sarcina);
                new JurnalRepository().Log(Session.CurrentUser!.Id, dlg.Sarcina.IdProiect, $"A adaugat sarcina '{dlg.Sarcina.Titlu}'", "Sarcina", id);
                LoadData();
            });
    }
}
