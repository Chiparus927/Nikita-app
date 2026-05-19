using System.Data;
using Valcongrup.Services;

namespace Valcongrup.UserControls;

public class UcUserManagement : UserControl
{
    private readonly UserApprovalService _approval = new();
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly Label _emptyState = new()
    {
        Dock = DockStyle.Fill,
        Text = "Nu exista utilizatori in sistem.",
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = AppTheme.TextOnLightSecondary,
        BackColor = Color.Transparent,
        Font = AppTheme.Font(11f, FontStyle.Regular),
        Visible = false
    };

    public UcUserManagement()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);

        var refresh = AppTheme.AccentButton("Reincarca", 130, 46);
        refresh.Click += (_, _) => LoadData();

        Controls.Add(BuildTable());
        Controls.Add(ModernUi.PageHeader(
            "User Management",
            "Gestioneaza conturile utilizatorilor si aprobarile de acces.",
            refresh));

        _grid.CellContentClick += Grid_CellContentClick;
        _grid.CellClick += Grid_CellContentClick;
        LoadData();
    }

    private void SafeUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
            BeginInvoke(action);
        else
            action();
    }

    private Control BuildTable()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 12,
            BackColor = AppTheme.Card,
            Padding = new Padding(0),
            Margin = new Padding(0, 20, 0, 0)
        };

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToOrderColumns = false;
        _grid.AllowUserToResizeColumns = false;
        _grid.AllowUserToResizeRows = false;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowTemplate.Height = 35;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

        card.Controls.Add(_grid);
        card.Controls.Add(_emptyState);
        _emptyState.BringToFront();
        return card;
    }

    private void LoadData()
    {
        UiFactory.Try(() =>
        {
            var table = _approval.GetUsersForManagement();
            if (!table.Columns.Contains("Companie"))
                table.Columns.Add("Companie", typeof(string)).DefaultValue = "-";

            void Bind()
            {
                _grid.DataSource = null;
                _grid.Columns.Clear();
                _grid.AutoGenerateColumns = false;

                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "user_id",
                    HeaderText = "ID",
                    DataPropertyName = "user_id",
                    Visible = false
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Nume",
                    HeaderText = "Nume complet",
                    DataPropertyName = "Nume",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 22,
                    MinimumWidth = 160
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Email",
                    HeaderText = "Email",
                    DataPropertyName = "Email",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 42,
                    MinimumWidth = 260
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Telefon",
                    HeaderText = "Telefon",
                    DataPropertyName = "Telefon",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    MinimumWidth = 120
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Rol",
                    HeaderText = "Rol",
                    DataPropertyName = "Rol",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    MinimumWidth = 110
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Companie",
                    HeaderText = "Companie",
                    DataPropertyName = "Companie",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 24,
                    MinimumWidth = 180
                });
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "StatusCont",
                    HeaderText = "Status",
                    DataPropertyName = "StatusCont",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    MinimumWidth = 130
                });
                _grid.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = "Approve",
                    HeaderText = "Aprobare",
                    DataPropertyName = "AprobareText",
                    UseColumnTextForButtonValue = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 110,
                    FlatStyle = FlatStyle.Flat
                });
                _grid.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = "Reject",
                    HeaderText = "Respingere",
                    DataPropertyName = "RespingereText",
                    UseColumnTextForButtonValue = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 118,
                    FlatStyle = FlatStyle.Flat
                });

                _grid.DataSource = table;
                _emptyState.Visible = table.Rows.Count == 0;
            }

            SafeUi(Bind);
        });
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var idObj = _grid.Rows[e.RowIndex].Cells["user_id"].Value;
        if (idObj == null)
            return;

        int id;
        try
        {
            id = Convert.ToInt32(idObj);
        }
        catch
        {
            return;
        }

        var col = _grid.Columns[e.ColumnIndex];
        if (col == null)
            return;

        var isButton = col is DataGridViewButtonColumn;
        var colName = col.Name ?? string.Empty;
        if (!isButton && colName != "Approve" && colName != "Reject")
            return;

        UiFactory.Try(() =>
        {
            bool ok;
            string msg;

            if (colName == "Approve" || isButton && col.HeaderText.Contains("Aprobare"))
            {
                var status = Convert.ToString(_grid.Rows[e.RowIndex].Cells["StatusCont"].Value) ?? string.Empty;
                if (string.Equals(status, "Aprobat", StringComparison.OrdinalIgnoreCase))
                    return;

                ok = _approval.ApproveUser(id);
                msg = ok ? "Utilizator aprobat cu succes." : "Aprobarea a esuat.";
            }
            else
            {
                var status = Convert.ToString(_grid.Rows[e.RowIndex].Cells["StatusCont"].Value) ?? string.Empty;
                if (string.Equals(status, "Respins/Suspendat", StringComparison.OrdinalIgnoreCase))
                    return;

                ok = _approval.RejectUser(id);
                msg = ok ? "Utilizator respins si sters." : "Respingerea a esuat.";
            }

            void ShowAndReload()
            {
                MessageBox.Show(
                    msg,
                    "VALCONGRUP",
                    MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                LoadData();
            }

            SafeUi(ShowAndReload);
        });
    }
}
