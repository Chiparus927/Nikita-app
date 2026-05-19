using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;

namespace Valcongrup.UserControls;

public class UcProiecte : UserControl
{
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly ProiecteRepository _repo = new();
    private readonly Button _statusFilter = ModernUi.GhostButton("Status: toate", 150);
    private readonly Button _managerFilter = ModernUi.GhostButton("Manager: toti", 170);
    private string? _selectedStatus;
    private string? _selectedManager;

    public UcProiecte()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);

        var add = AppTheme.AccentButton("+ Adauga Proiect", 210, 46);
        add.Visible = Session.IsManagerOrAdmin();
        add.Click += (_, _) => Add();

        var request = AppTheme.AccentButton("Cerere proiect", 180, 46);
        request.Visible = Session.CanRequestProjects();
        request.Click += (_, _) => RequestProject();
        _statusFilter.Click += (_, _) => ShowStatusFilter();
        _managerFilter.Click += (_, _) => ShowManagerFilter();

        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader(
            "Gestionare Proiecte",
            "Administreaza si monitorizeaza proiectele de constructii.",
            _statusFilter,
            _managerFilter,
            request,
            add));

        _grid.CellFormatting += GridFormatting;
        _grid.CellPainting += (_, e) => ModernUi.PaintStatusBadge(_grid, e);
        LoadData();
    }

    private Control BuildTable()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            BackColor = AppTheme.Card,
            Padding = new Padding(0),
            Margin = new Padding(0, 20, 0, 0)
        };

        _grid.Location = new Point(0, 0);
        _grid.Size = card.Size;
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        card.Controls.Add(_grid);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 74, BackColor = AppTheme.Card };
        var count = ModernUi.Text("Afisand proiectele active", 10f, AppTheme.MutedBlue);
        count.Location = new Point(36, 26);
        var prev = ModernUi.GhostButton("Anterior", 106);
        prev.Height = 40;
        var next = ModernUi.GhostButton("Urmator", 106);
        next.Height = 40;
        prev.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        footer.Controls.AddRange(new Control[] { count, prev, next });
        footer.Resize += (_, _) =>
        {
            next.Location = new Point(footer.Width - 140, 17);
            prev.Location = new Point(footer.Width - 260, 17);
        };
        card.Controls.Add(footer);
        return card;
    }

    private void LoadData()
    {
        UiFactory.Try(() =>
        {
            var user = Session.CurrentUser;
            _grid.DataSource = user == null
                ? _repo.LoadProjectsData()
                : _repo.LoadProjectsDataForUser(user.Id, Session.RoleName, _selectedStatus, _selectedManager);
        });
    }

    private void ShowStatusFilter()
    {
        var user = Session.CurrentUser;
        if (user == null)
            return;

        UiFactory.Try(() =>
        {
            var options = _repo.GetProjectStatusesForUser(user.Id, Session.RoleName);
            ShowFilterMenu(_statusFilter, options, _selectedStatus, selected =>
            {
                _selectedStatus = string.IsNullOrWhiteSpace(selected) ? null : selected;
                _statusFilter.Text = string.IsNullOrWhiteSpace(_selectedStatus) ? "Status: toate" : $"Status: {_selectedStatus}";
                LoadData();
            });
        });
    }

    private void ShowManagerFilter()
    {
        var user = Session.CurrentUser;
        if (user == null)
            return;

        UiFactory.Try(() =>
        {
            var options = _repo.GetManagersForUser(user.Id, Session.RoleName);
            ShowFilterMenu(_managerFilter, options, _selectedManager, selected =>
            {
                _selectedManager = string.IsNullOrWhiteSpace(selected) ? null : selected;
                string? label = null;
                foreach (System.Data.DataRow row in options.Rows)
                {
                    if (!string.Equals(Convert.ToString(row["value"]), _selectedManager ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                        continue;
                    label = Convert.ToString(row["label"]);
                    break;
                }
                _managerFilter.Text = string.IsNullOrWhiteSpace(_selectedManager) ? "Manager: toti" : $"Manager: {label}";
                LoadData();
            });
        });
    }

    private static void ShowFilterMenu(Control owner, System.Data.DataTable options, string? selectedValue, Action<string?> select)
    {
        var menu = new ContextMenuStrip();
        foreach (System.Data.DataRow row in options.Rows)
        {
            var value = Convert.ToString(row["value"]) ?? string.Empty;
            var label = Convert.ToString(row["label"]) ?? value;
            var item = new ToolStripMenuItem(label)
            {
                Checked = string.Equals(value, selectedValue ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => select(value);
            menu.Items.Add(item);
        }

        menu.Show(owner, new Point(0, owner.Height + 4));
    }

    private void GridFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0)
            return;

        var header = _grid.Columns[e.ColumnIndex].HeaderText;
        if (header == "Buget Total")
            e.CellStyle!.ForeColor = AppTheme.Accent;
        if (header == "Procent Finalizat")
            e.CellStyle!.ForeColor = AppTheme.TextOnLight;
        if (header == "Status")
            e.CellStyle!.ForeColor = AppTheme.TextOnLight;
        if (header == "Nume Proiect")
            e.CellStyle!.Font = AppTheme.Font(10f, FontStyle.Bold);
    }

    private void Add()
    {
        if (!Session.IsManagerOrAdmin())
        {
            MessageBox.Show("Doar rolurile Manager si Admin pot adauga proiecte.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new DialogProiect();
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        UiFactory.Try(() =>
        {
            var id = _repo.Insert(dlg.Proiect);
            new JurnalRepository().Log(Session.CurrentUser!.Id, id, $"A adaugat proiectul '{dlg.Proiect.Nume}'", "Proiect", id);
            LoadData();
        });
    }

    private void RequestProject()
    {
        if (!Session.CanRequestProjects())
        {
            MessageBox.Show("Doar clientii si subcontractorii pot trimite cereri de proiect.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new DialogProjectRequest();
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        UiFactory.Try(() =>
        {
            var id = _repo.CreateClientRequest(Session.CurrentUser!.Id, dlg.NumeProiect, dlg.Descriere, dlg.BugetEstimat, dlg.TermenLimita);
            new JurnalRepository().Log(Session.CurrentUser!.Id, id, $"A trimis cererea de proiect '{dlg.NumeProiect}'", "Proiect", id);
            MessageBox.Show("Cererea a fost trimisa catre manageri.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        });
    }
}
