namespace Valcongrup.Forms.Dialogs;

public class DialogProjectRequest : Form
{
    private readonly TextBox _nume = AppTheme.TextBox();
    private readonly TextBox _descriere = AppTheme.TextBox();
    private readonly NumericUpDown _buget = new() { Maximum = 1_000_000_000, DecimalPlaces = 2, Width = 220 };
    private readonly DateTimePicker _termen = new() { Format = DateTimePickerFormat.Short };

    public string NumeProiect => _nume.Text.Trim();
    public string Descriere => _descriere.Text.Trim();
    public decimal BugetEstimat => _buget.Value;
    public DateTime TermenLimita => _termen.Value.Date;

    public DialogProjectRequest()
    {
        Text = "Cerere proiect nou";
        Size = new Size(460, 430);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Shell;
        Font = AppTheme.Font();

        var body = new RoundedPanelShim { Dock = DockStyle.Fill, Padding = new Padding(26), BackColor = Color.White };
        Controls.Add(body);

        var title = new Label { Text = "Trimite cerere proiect", Font = AppTheme.Font(16f, FontStyle.Bold), ForeColor = AppTheme.TextOnLight, AutoSize = true, Location = new Point(26, 22) };
        body.Controls.Add(title);
        body.Controls.Add(Field("Nume Proiect", _nume, 26, 78, 380));
        body.Controls.Add(Field("Descriere", _descriere, 26, 146, 380));
        body.Controls.Add(Field("Buget estimat", _buget, 26, 214, 180));
        body.Controls.Add(Field("Termen limita", _termen, 226, 214, 180));

        var save = AppTheme.AccentButton("Trimite", 140, 44);
        var cancel = AppTheme.SecondaryButton("Anuleaza", 120, 44);
        save.Location = new Point(266, 318);
        cancel.Location = new Point(136, 318);
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nume.Text) || _buget.Value <= 0)
            {
                MessageBox.Show("Completeaza numele proiectului si bugetul estimat.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
        };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        body.Controls.AddRange(new Control[] { cancel, save });
    }

    private static Control Field(string label, Control input, int x, int y, int width)
    {
        var p = new Panel { Location = new Point(x, y), Size = new Size(width, 58), BackColor = Color.White };
        p.Controls.Add(new Label { Text = label, Location = new Point(0, 0), AutoSize = true, ForeColor = AppTheme.TextOnLightSecondary, Font = AppTheme.Font(9f, FontStyle.Bold), BackColor = Color.White });
        input.Location = new Point(0, 24);
        input.Width = width;
        p.Controls.Add(input);
        return p;
    }

    private sealed class RoundedPanelShim : Panel
    {
        public RoundedPanelShim() => DoubleBuffered = true;
    }
}
