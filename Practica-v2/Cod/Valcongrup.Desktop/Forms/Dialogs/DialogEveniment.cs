using Valcongrup.Models;
using Valcongrup.UserControls;

namespace Valcongrup.Forms.Dialogs;

public class DialogEveniment : Form
{
    private readonly TextBox _titlu = AppTheme.TextBox();
    private readonly TextBox _descriere = AppTheme.TextBox();
    private readonly ComboBox _tip = AppTheme.Combo();
    private readonly ComboBox _proiect = AppTheme.Combo();
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm" };
    private readonly DateTimePicker _sfarsit = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm" };
    public Eveniment Eveniment { get; } = new();

    public DialogEveniment(DateTime data)
    {
        Text = "Adauga Eveniment";
        Size = new Size(460, 540);
        MinimumSize = new Size(460, 540);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.BgDark;
        Padding = new Padding(18);
        _tip.Items.AddRange(new object[] { "Livrare", "Inspectie", "Sedinta", "Termen", "Altul" }); _tip.SelectedIndex = 4;
        _start.Value = data.Date.AddHours(9); _sfarsit.Value = data.Date.AddHours(10);
        UiFactory.Try(() => { _proiect.DataSource = UiFactory.ProjectsLookup(); _proiect.DisplayMember = "nume"; _proiect.ValueMember = "id"; });
        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgDark, AutoScroll = true }; Controls.Add(body);
        body.Controls.AddRange(new Control[] { Buttons(), UiFactory.Field("Data Sfarsit", _sfarsit), UiFactory.Field("Data Start", _start), UiFactory.Field("Proiect", _proiect), UiFactory.Field("Tip", _tip), UiFactory.Field("Descriere", _descriere), UiFactory.Field("Titlu*", _titlu) });
    }

    private Control Buttons()
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.BgDark };
        var save = AppTheme.PrimaryButton("Salveaza"); var cancel = AppTheme.SecondaryButton("Anuleaza");
        save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_titlu.Text)) { MessageBox.Show("Titlu obligatoriu."); return; } Eveniment.Titlu = _titlu.Text.Trim(); Eveniment.Descriere = _descriere.Text.Trim(); Eveniment.Tip = _tip.Text; Eveniment.IdProiect = _proiect.SelectedValue == null ? null : Convert.ToInt32(_proiect.SelectedValue); Eveniment.DataStart = _start.Value; Eveniment.DataSfarsit = _sfarsit.Value; DialogResult = DialogResult.OK; };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        p.Controls.AddRange(new Control[] { save, cancel }); return p;
    }
}
