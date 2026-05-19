using System.Data;
using Valcongrup.Models;
using Valcongrup.UserControls;

namespace Valcongrup.Forms.Dialogs;

public class DialogProiect : Form
{
    private readonly TextBox _nume = AppTheme.TextBox();
    private readonly TextBox _descriere = AppTheme.TextBox();
    private readonly ComboBox _client = AppTheme.Combo();
    private readonly ComboBox _manager = AppTheme.Combo();
    private readonly NumericUpDown _buget = new() { Maximum = 1000000000, DecimalPlaces = 2, Width = 160 };
    private readonly DateTimePicker _start = new();
    private readonly DateTimePicker _termen = new();
    private readonly ComboBox _status = AppTheme.Combo();
    private readonly TrackBar _progres = new() { Minimum = 0, Maximum = 100, TickFrequency = 10 };
    public Proiect Proiect { get; private set; }

    public DialogProiect(Proiect? proiect = null)
    {
        Proiect = proiect ?? new Proiect();
        Text = proiect == null ? "Adauga Proiect" : "Editeaza Proiect";
        ClientSize = new Size(560, 690);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = AppTheme.BgDark;
        ForeColor = AppTheme.TextPrimary;
        Padding = new Padding(24);
        MinimizeBox = false;
        MaximizeBox = false;

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.BgDark,
            ColumnCount = 1,
            RowCount = 10,
            Padding = new Padding(0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 9; i++)
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 8 ? 74 : 62));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(body);
        _status.Items.AddRange(new object[] { "Activ", "Planificat", "Suspendat", "Finalizat" });
        LoadLookups();
        _start.Format = DateTimePickerFormat.Short;
        _termen.Format = DateTimePickerFormat.Short;
        body.Controls.Add(Field("Nume*", _nume), 0, 0);
        body.Controls.Add(Field("Descriere", _descriere), 0, 1);
        body.Controls.Add(Field("Client", _client), 0, 2);
        body.Controls.Add(Field("Manager*", _manager), 0, 3);
        body.Controls.Add(Field("Buget Total", _buget), 0, 4);
        body.Controls.Add(Field("Data Start", _start), 0, 5);
        body.Controls.Add(Field("Data Termen", _termen), 0, 6);
        body.Controls.Add(Field("Status", _status), 0, 7);
        body.Controls.Add(Field("Progres", _progres), 0, 8);
        body.Controls.Add(Buttons(), 0, 9);
        Bind();
    }

    private static Panel Field(string label, Control input)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgDark, Padding = new Padding(0, 0, 0, 10) };
        var l = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = AppTheme.TextSecond,
            BackColor = AppTheme.BgDark,
            Font = AppTheme.Font(9f)
        };
        input.Dock = DockStyle.Fill;
        input.Font = AppTheme.Font(10f);
        p.Controls.Add(input);
        p.Controls.Add(l);
        return p;
    }

    private void LoadLookups()
    {
        UiFactory.Try(() =>
        {
            _client.DataSource = UiFactory.ClientsLookup(); _client.DisplayMember = "nume"; _client.ValueMember = "id";
            _manager.DataSource = UiFactory.UsersLookup(); _manager.DisplayMember = "nume"; _manager.ValueMember = "id";
        });
    }

    private void Bind()
    {
        _nume.Text = Proiect.Nume; _descriere.Text = Proiect.Descriere; _buget.Value = Math.Min(_buget.Maximum, Proiect.BugetTotal);
        _start.Value = Proiect.DataStart; _termen.Value = Proiect.DataTermen; _status.Text = Proiect.Status; _progres.Value = Math.Clamp(Proiect.Progres, 0, 100);
        if (Proiect.IdClient.HasValue) _client.SelectedValue = Proiect.IdClient.Value;
        if (Proiect.IdManager.HasValue && Proiect.IdManager.Value > 0) _manager.SelectedValue = Proiect.IdManager.Value;
    }

    private Control Buttons()
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.BgDark, Padding = new Padding(0, 8, 0, 0) };
        var save = AppTheme.PrimaryButton("Salveaza", 140, 42);
        var cancel = AppTheme.SecondaryButton("Anuleaza", 120, 42);
        save.Margin = new Padding(8, 0, 0, 0);
        cancel.Margin = new Padding(8, 0, 0, 0);
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        p.Controls.AddRange(new Control[] { save, cancel });
        return p;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_nume.Text)) { MessageBox.Show("Nume obligatoriu."); return; }
        if (_termen.Value.Date <= _start.Value.Date) { MessageBox.Show("Data termen trebuie sa fie dupa data start."); return; }
        if (_buget.Value <= 0) { MessageBox.Show("Bugetul trebuie sa fie mai mare decat 0."); return; }
        Proiect.Nume = _nume.Text.Trim();
        Proiect.Descriere = _descriere.Text.Trim();
        Proiect.IdClient = _client.SelectedValue == null ? null : Convert.ToInt32(_client.SelectedValue);
        Proiect.IdManager = Convert.ToInt32(_manager.SelectedValue);
        Proiect.BugetTotal = _buget.Value;
        Proiect.DataStart = _start.Value.Date;
        Proiect.DataTermen = _termen.Value.Date;
        Proiect.Status = _status.Text;
        Proiect.Progres = _progres.Value;
        DialogResult = DialogResult.OK;
    }
}
