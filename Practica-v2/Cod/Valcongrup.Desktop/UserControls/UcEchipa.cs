using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;
using Valcongrup.Models;
using MySql.Data.MySqlClient;

namespace Valcongrup.UserControls;

public class UcEchipa : UserControl
{
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly UtilizatoriRepository _repo = new();
    private readonly Button _specialization;

    public UcEchipa()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);

        _specialization = AppTheme.AccentButton("Specializare", 170, 46);
        _specialization.Click += (_, _) => ShowSpecializationFilter();

        var add = AppTheme.AccentButton("+  Invită membru", 200, 46);
        add.Visible = Session.IsManagerOrAdmin();
        add.Click += (_, _) => Add();

        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader("Echipă", "Gestionează membrii echipei și alocările lor pe proiecte.", _specialization, add));
        ConfigureGrid();
        LoadData();
    }

    private Control BuildTable()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 18, BackColor = AppTheme.Card, Padding = new Padding(0), Margin = new Padding(0, 20, 0, 0) };
        card.Controls.Add(_grid);
        return card;
    }

    private void ConfigureGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Membru", DataPropertyName = "FullName", FillWeight = 28, MinimumWidth = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rol", DataPropertyName = "NumeRol", FillWeight = 18, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Email", DataPropertyName = "Email", FillWeight = 30, MinimumWidth = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Telefon", DataPropertyName = "Telefon", FillWeight = 14, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "StatusText", FillWeight = 10, MinimumWidth = 100 });
        _grid.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "Editare", Text = "Editeaza", UseColumnTextForButtonValue = true, FillWeight = 10, MinimumWidth = 105, FlatStyle = FlatStyle.Flat });
        _grid.Columns.Add(new DataGridViewButtonColumn { Name = "Toggle", HeaderText = "Activ", DataPropertyName = "ToggleText", UseColumnTextForButtonValue = false, FillWeight = 10, MinimumWidth = 120, FlatStyle = FlatStyle.Flat });
        _grid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "Stergere", Text = "Sterge", UseColumnTextForButtonValue = true, FillWeight = 10, MinimumWidth = 100, FlatStyle = FlatStyle.Flat });
        _grid.CellPainting += Grid_CellPainting;
        _grid.CellClick += Grid_CellContentClick;
        foreach (DataGridViewColumn col in _grid.Columns)
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
    }

    private void LoadData()
    {
        UiFactory.Try(() =>
        {
            _grid.DataSource = GetVisibleUsers()
                .Select(u => new TeamRow(u))
                .ToList();
        });
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("Edit" or "Toggle" or "Delete"))
            return;

        var idValue = _grid.Rows[e.RowIndex].Cells["Id"].Value;
        if (!int.TryParse(Convert.ToString(idValue), out var userId))
            return;

        if (columnName == "Edit")
            Edit(userId);
        else if (columnName == "Toggle")
            ToggleActive(userId);
        else
            Delete(userId);
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        ModernUi.PaintStatusBadge(_grid, e);
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("Edit" or "Toggle" or "Delete"))
            return;
        var g = e.Graphics;
        if (g == null)
            return;

        e.Handled = true;
        e.PaintBackground(e.ClipBounds, true);
        using var rowPen = new Pen(AppTheme.Border, 1);
        g.DrawLine(rowPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

        var text = Convert.ToString(e.Value);
        if (string.IsNullOrWhiteSpace(text))
            text = columnName == "Edit" ? "Editeaza" : columnName == "Delete" ? "Sterge" : "Suspenda";

        var fill = columnName switch
        {
            "Edit" => Color.FromArgb(255, 237, 213),
            "Toggle" => text.StartsWith("React", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(209, 250, 229) : Color.FromArgb(255, 247, 237),
            "Delete" => Color.FromArgb(254, 226, 226),
            _ => AppTheme.CardAlt
        };
        var fore = columnName switch
        {
            "Edit" => AppTheme.Accent,
            "Toggle" => text.StartsWith("React", StringComparison.OrdinalIgnoreCase) ? AppTheme.Success : AppTheme.TextOnLight,
            "Delete" => AppTheme.Danger,
            _ => AppTheme.TextOnLight
        };

        var width = Math.Min(e.CellBounds.Width - 18, Math.Max(76, TextRenderer.MeasureText(text, AppTheme.Font(8.5f, FontStyle.Bold)).Width + 24));
        var rect = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - width) / 2, e.CellBounds.Y + (e.CellBounds.Height - 30) / 2, width, 30);
        using var path = AppTheme.RoundedPath(rect, 15);
        using var brush = new SolidBrush(fill);
        using var border = new Pen(Color.FromArgb(230, fore), 1);
        g.FillPath(brush, path);
        g.DrawPath(border, path);
        TextRenderer.DrawText(g, text, AppTheme.Font(8.5f, FontStyle.Bold), rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void Edit(int userId)
    {
        var user = GetVisibleUsers().FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            MessageBox.Show("Utilizatorul nu a fost gasit.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadData();
            return;
        }

        using var dlg = new DialogUtilizator(user);
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        UiFactory.Try(() =>
        {
            _repo.Update(dlg.Utilizator);
            if (!string.IsNullOrWhiteSpace(dlg.Parola))
                _repo.UpdateParola(dlg.Utilizator.Id, Helpers.PasswordHelper.Hash(dlg.Parola));
            new JurnalRepository().Log(Session.CurrentUser!.Id, null, $"A editat utilizatorul '{dlg.Utilizator.Email}'", "Utilizator", dlg.Utilizator.Id);
            LoadData();
        });
    }

    private void ToggleActive(int userId)
    {
        var user = GetVisibleUsers().FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return;

        if (Session.CurrentUser?.Id == userId && user.Activ)
        {
            MessageBox.Show("Nu iti poti suspenda propriul cont.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var next = !user.Activ;
        UiFactory.Try(() =>
        {
            _repo.SetActiv(userId, next);
            new JurnalRepository().Log(Session.CurrentUser!.Id, null, next ? $"A reactivat utilizatorul '{user.Email}'" : $"A suspendat utilizatorul '{user.Email}'", "Utilizator", userId);
            LoadData();
        });
    }

    private void Delete(int userId)
    {
        var user = GetVisibleUsers().FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return;

        if (Session.CurrentUser?.Id == userId)
        {
            MessageBox.Show("Nu iti poti sterge propriul cont.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"Stergi utilizatorul {user.Prenume} {user.Nume}?", "Confirmare", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            if (!_repo.Delete(userId))
            {
                MessageBox.Show("Stergerea a esuat.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            new JurnalRepository().Log(Session.CurrentUser!.Id, null, $"A sters utilizatorul '{user.Email}'", "Utilizator", userId);
            LoadData();
        }
        catch (MySqlException ex) when (ex.Number == 1451 || ex.Number == 1452)
        {
            if (MessageBox.Show("Utilizatorul are legaturi in proiecte sau activitati. Vrei sa il suspendi in loc sa il stergi?", "VALCONGRUP", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            UiFactory.Try(() =>
            {
                _repo.SetActiv(userId, false);
                new JurnalRepository().Log(Session.CurrentUser!.Id, null, $"A suspendat utilizatorul '{user.Email}'", "Utilizator", userId);
                LoadData();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Eroare: " + ex.Message, "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Add()
    {
        using var dlg = new DialogUtilizator();
        if (dlg.ShowDialog() == DialogResult.OK)
            UiFactory.Try(() =>
            {
                var id = _repo.Insert(dlg.Utilizator, Helpers.PasswordHelper.Hash(dlg.Parola));
                new JurnalRepository().Log(Session.CurrentUser!.Id, null, $"A adaugat utilizatorul '{dlg.Utilizator.Email}'", "Utilizator", id);
                LoadData();
            });
    }

    private void ShowSpecializationFilter()
    {
        var roles = GetVisibleUsers()
            .Select(u => u.NumeRol)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();

        if (roles.Count == 0)
        {
            MessageBox.Show("Nu există specializări definite în acest moment.", "Specializare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new Form
        {
            Text = "Filtrează specializare",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(420, 160)
        };

        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = AppTheme.Font(10f), Height = 36, Margin = new Padding(16) };
        combo.Items.Add("Toate specializările");
        combo.Items.AddRange(roles.Cast<object>().ToArray());
        combo.SelectedIndex = 0;

        var label = new Label
        {
            Text = "Alege specializarea:",
            Dock = DockStyle.Top,
            Font = AppTheme.Font(10f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(16, 16, 16, 4)
        };

        var apply = AppTheme.AccentButton("Aplică", 120, 42);
        apply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        apply.Location = new Point(dialog.ClientSize.Width - apply.Width - 16, dialog.ClientSize.Height - apply.Height - 16);
        apply.Click += (_, _) => dialog.DialogResult = DialogResult.OK;

        dialog.Controls.Add(apply);
        dialog.Controls.Add(combo);
        dialog.Controls.Add(label);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        var selected = combo.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(selected) || selected == "Toate specializările")
            LoadData();
        else
            UiFactory.Try(() =>
            {
                _grid.DataSource = GetVisibleUsers()
                    .Where(u => string.Equals(u.NumeRol, selected, StringComparison.OrdinalIgnoreCase))
                    .Select(u => new TeamRow(u))
                    .ToList();
            });
    }

    private List<Utilizator> GetVisibleUsers()
    {
        if (Session.IsAdmin())
            return _repo.GetAll();
        if (Session.IsManager() && Session.CurrentUser != null)
            return _repo.GetParticipantsForManager(Session.CurrentUser.Id);
        return _repo.GetAll();
    }

    private sealed class TeamRow
    {
        public TeamRow(Utilizator user)
        {
            Id = user.Id;
            FullName = $"{user.Prenume} {user.Nume}".Trim();
            NumeRol = user.NumeRol;
            Email = user.Email;
            Telefon = string.IsNullOrWhiteSpace(user.Telefon) ? "-" : user.Telefon;
            StatusText = user.Activ ? "Activ" : "Suspendat";
            ToggleText = user.Activ ? "Suspenda" : "Reactiveaza";
        }

        public int Id { get; }
        public string FullName { get; }
        public string NumeRol { get; }
        public string Email { get; }
        public string Telefon { get; }
        public string StatusText { get; }
        public string ToggleText { get; }
    }
}
