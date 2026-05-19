using Valcongrup.Data;
using Valcongrup.Models;

namespace Valcongrup.UserControls;

public class UcDocumente : UserControl
{
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly ComboBox _proiect = AppTheme.Combo();
    private readonly ComboBox _categorie = AppTheme.Combo();

    public UcDocumente()
    {
        BackColor = AppTheme.Shell; Padding = new Padding(48, 0, 32, 32);
        var add = AppTheme.AccentButton("Încarcă Document", 190, 54); add.Click += (_, _) => Incarca();
        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader("Documente", "", ModernUi.GhostButton("Centru Comercial Nord", 230), ModernUi.GhostButton("Altele", 120), add));
        _grid.CellDoubleClick += (_, _) =>
        {
            if (_grid.CurrentRow?.Cells["cale_fișier"]?.Value is string cale && File.Exists(cale))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = cale, UseShellExecute = true });
        };
        LoadData();
    }
    private Control BuildTable()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 12, BackColor = AppTheme.Card, Padding = new Padding(0), Margin = new Padding(0, 20, 0, 0) };
        _grid.Dock = DockStyle.Fill;
        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].HeaderText == "Format")
            {
                e.CellStyle!.ForeColor = e.Value?.ToString() == "PDF" ? Color.FromArgb(239, 68, 68) : Color.FromArgb(59, 130, 246);
                e.CellStyle.Font = AppTheme.Font(10f, FontStyle.Bold);
            }
        };
        card.Controls.Add(_grid);
        return card;
    }
    private void LoadData() => UiFactory.Try(() => _grid.DataSource = new DocumenteRepository().GetAll());
    private void Incarca()
    {
        using var ofd = new OpenFileDialog { Filter = "Toate fisierele|*.*|PDF|*.pdf|DWG|*.dwg|Excel|*.xlsx|Imagini|*.jpg;*.png" };
        if (ofd.ShowDialog() != DialogResult.OK || _proiect.SelectedValue == null || _categorie.SelectedValue == null) return;
        UiFactory.Try(() =>
        {
            Directory.CreateDirectory("Documente");
            var dest = Path.Combine("Documente", Path.GetFileName(ofd.FileName));
            File.Copy(ofd.FileName, dest, true);
            var doc = new Document { IdProiect = Convert.ToInt32(_proiect.SelectedValue), IdCategorie = Convert.ToInt32(_categorie.SelectedValue), IdIncarcatDe = Session.CurrentUser!.Id, NumeFisier = Path.GetFileName(ofd.FileName), CaleFisier = dest, Format = Path.GetExtension(ofd.FileName).TrimStart('.').ToUpperInvariant(), DimensiuneKb = (int)(new FileInfo(ofd.FileName).Length / 1024) };
            var id = new DocumenteRepository().Insert(doc);
            new JurnalRepository().Log(Session.CurrentUser!.Id, doc.IdProiect, $"A incarcat documentul '{doc.NumeFisier}'", "Document", id);
            LoadData();
        });
    }
}
