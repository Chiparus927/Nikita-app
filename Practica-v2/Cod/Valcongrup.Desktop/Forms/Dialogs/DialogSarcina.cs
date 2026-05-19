using Valcongrup.Models;
using Valcongrup.UserControls;

namespace Valcongrup.Forms.Dialogs;

public class DialogSarcina : Form
{
    private readonly TextBox _titlu = AppTheme.TextBox();
    private readonly TextBox _descriere = AppTheme.TextBox();
    private readonly ComboBox _proiect = AppTheme.Combo();
    private readonly ComboBox _responsabil = AppTheme.Combo();
    private readonly ComboBox _prioritate = AppTheme.Combo();
    private readonly ComboBox _status = AppTheme.Combo();
    private readonly DateTimePicker _termen = new() { Format = DateTimePickerFormat.Short };
    public Sarcina Sarcina { get; private set; }

    public DialogSarcina(Sarcina? s = null)
    {
        Sarcina = s ?? new Sarcina();
        Text = s == null ? "Adauga Sarcina" : "Editeaza Sarcina";
        Size = new Size(480, 590);
        MinimumSize = new Size(480, 590);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.BgDark;
        Padding = new Padding(18);
        _prioritate.Items.AddRange(new object[] { "Urgenta", "Ridicata", "Medie", "Scazuta" });
        _status.Items.AddRange(new object[] { "Noua", "In_Progres", "Blocata", "Finalizata" });
        UiFactory.Try(() => { _proiect.DataSource = UiFactory.ProjectsLookup(); _proiect.DisplayMember = "nume"; _proiect.ValueMember = "id"; _responsabil.DataSource = UiFactory.UsersLookup(); _responsabil.DisplayMember = "nume"; _responsabil.ValueMember = "id"; });
        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgDark, AutoScroll = true }; Controls.Add(body);
        body.Controls.AddRange(new Control[] { Buttons(), UiFactory.Field("Data Termen", _termen), UiFactory.Field("Status", _status), UiFactory.Field("Prioritate", _prioritate), UiFactory.Field("Responsabil", _responsabil), UiFactory.Field("Proiect*", _proiect), UiFactory.Field("Descriere", _descriere), UiFactory.Field("Titlu*", _titlu) });
        _titlu.Text = Sarcina.Titlu; _descriere.Text = Sarcina.Descriere; _prioritate.Text = Sarcina.Prioritate; _status.Text = Sarcina.Status; if (Sarcina.DataTermen.HasValue) _termen.Value = Sarcina.DataTermen.Value; if (Sarcina.IdProiect > 0) _proiect.SelectedValue = Sarcina.IdProiect; if (Sarcina.IdResponsabil.HasValue) _responsabil.SelectedValue = Sarcina.IdResponsabil.Value;
    }

    private Control Buttons()
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.BgDark };
        var save = AppTheme.PrimaryButton("Salveaza"); var cancel = AppTheme.SecondaryButton("Anuleaza");
        save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_titlu.Text) || _proiect.SelectedValue == null) { MessageBox.Show("Titlu si proiect obligatorii."); return; } Sarcina.Titlu = _titlu.Text.Trim(); Sarcina.Descriere = _descriere.Text.Trim(); Sarcina.IdProiect = Convert.ToInt32(_proiect.SelectedValue); Sarcina.IdResponsabil = _responsabil.SelectedValue == null ? null : Convert.ToInt32(_responsabil.SelectedValue); Sarcina.Prioritate = _prioritate.Text; Sarcina.Status = _status.Text; Sarcina.DataTermen = _termen.Value.Date; DialogResult = DialogResult.OK; };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        p.Controls.AddRange(new Control[] { save, cancel });
        return p;
    }
}
