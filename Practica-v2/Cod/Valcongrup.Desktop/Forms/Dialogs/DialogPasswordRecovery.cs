using Valcongrup.Data;
using Valcongrup.Helpers;

namespace Valcongrup.Forms.Dialogs;

public class DialogPasswordRecovery : Form
{
    private static readonly Color TextDark = Color.FromArgb(20, 28, 36);
    private static readonly Color TextMuted = Color.FromArgb(90, 98, 112);
    private static readonly Color BorderLight = Color.FromArgb(220, 224, 230);

    private readonly TextBox _email = AppTheme.TextBox();
    private readonly TextBox _newPassword = AppTheme.TextBox();
    private readonly TextBox _confirmPassword = AppTheme.TextBox();
    private readonly Label _message = new()
    {
        ForeColor = AppTheme.Danger,
        AutoSize = true,
        MaximumSize = new Size(520, 0),
        BackColor = Color.White,
        TextAlign = ContentAlignment.TopLeft
    };
    private readonly Panel _emailBox;
    private readonly Panel _newPasswordBox;
    private readonly Panel _confirmPasswordBox;
    private readonly UtilizatoriRepository _repo = new();
    private readonly FlowLayoutPanel _body;

    public DialogPasswordRecovery()
    {
        Text = "Resetare parolă";
        Size = new Size(640, 780);
        MinimumSize = new Size(560, 700);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;

        _newPassword.UseSystemPasswordChar = true;
        _confirmPassword.UseSystemPasswordChar = true;

        _emailBox = InputPanel(_email);
        _newPasswordBox = InputPanel(_newPassword);
        _confirmPasswordBox = InputPanel(_confirmPassword);

        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20) };
        Controls.Add(outer);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(28, 24, 28, 28),
            BorderStyle = BorderStyle.None,
            AutoScroll = false
        };
        card.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            using var pen = new Pen(BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        outer.Controls.Add(card);

        var title = new Label
        {
            Text = "Ai uitat parola?",
            Font = AppTheme.Font(20f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            BackColor = Color.White
        };
        var subtitle = new Label
        {
            Text = "Introdu emailul tău înregistrat și alege o parolă nouă pentru cont.",
            Font = AppTheme.Font(11f),
            ForeColor = TextMuted,
            AutoSize = true,
            BackColor = Color.White,
            UseMnemonic = false
        };
        var hint = new Label
        {
            Text = "Parola trebuie să aibă cel puțin 8 caractere și să se potrivească cu confirmarea.",
            Font = AppTheme.Font(9.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            BackColor = Color.White,
            UseMnemonic = false
        };

        var titleBlock = new Panel { BackColor = Color.White, Margin = new Padding(0, 0, 0, 12) };
        titleBlock.Controls.Add(title);
        titleBlock.Controls.Add(subtitle);
        title.Location = new Point(0, 0);

        _body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.White,
            Padding = new Padding(0, 8, 0, 8)
        };

        void SyncBodyWidths()
        {
            var w = Math.Max(240, _body.ClientSize.Width - _body.Padding.Horizontal);
            titleBlock.SuspendLayout();
            titleBlock.Width = w;
            title.MaximumSize = new Size(w, 0);
            title.Location = new Point(0, 0);
            var titleH = title.GetPreferredSize(new Size(w, 0)).Height;

            subtitle.MaximumSize = new Size(w, 0);
            subtitle.Location = new Point(0, titleH + 10);
            var subtitleH = subtitle.GetPreferredSize(new Size(w, 0)).Height;
            titleBlock.Height = titleH + 10 + subtitleH + 12;
            titleBlock.ResumeLayout(true);

            hint.MaximumSize = new Size(w, 0);
            _message.MaximumSize = new Size(w, 0);

            foreach (Control c in _body.Controls)
            {
                if (c == titleBlock)
                {
                    continue;
                }

                if (c is Label lab)
                {
                    lab.MaximumSize = new Size(w, 0);
                    continue;
                }

                c.Width = w;
                if (c is Panel p && p.Controls.Count >= 2 && p.Controls[1] is Panel innerBox)
                {
                    innerBox.Width = w;
                    var fieldLabel = p.Controls[0];
                    p.Height = fieldLabel.Bottom + 8 + innerBox.Height;
                }
            }

            _body.PerformLayout();
        }

        _body.SizeChanged += (_, _) => SyncBodyWidths();
        _body.Controls.Add(titleBlock);

        var emailField = CreateField("Email", _emailBox);
        emailField.Margin = new Padding(0, 6, 0, 0);
        _body.Controls.Add(emailField);

        var newPassField = CreateField("Parolă nouă", _newPasswordBox);
        newPassField.Margin = new Padding(0, 18, 0, 0);
        _body.Controls.Add(newPassField);

        var confirmField = CreateField("Confirmă parola", _confirmPasswordBox);
        confirmField.Margin = new Padding(0, 18, 0, 0);
        _body.Controls.Add(confirmField);

        hint.Margin = new Padding(0, 18, 0, 0);
        _body.Controls.Add(hint);

        _message.Margin = new Padding(0, 14, 0, 0);
        _body.Controls.Add(_message);

        _body.HandleCreated += (_, _) => BeginInvoke(SyncBodyWidths);
        Shown += (_, _) => SyncBodyWidths();

        var reset = AppTheme.AccentButton("Resetează parola", 380, 48);
        reset.Font = AppTheme.Font(11f, FontStyle.Bold);
        reset.Click += (_, _) => ResetPassword();
        var cancel = AppTheme.SecondaryButton("Anulează", 128, 48);
        cancel.ForeColor = TextDark;
        cancel.FlatAppearance.BorderColor = BorderLight;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        AcceptButton = reset;
        CancelButton = cancel;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 88,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 16, 0, 8),
            Margin = new Padding(0),
            BackColor = Color.White
        };
        buttonPanel.Controls.Add(reset);
        buttonPanel.Controls.Add(cancel);

        card.Controls.Add(buttonPanel);
        card.Controls.Add(_body);
    }

    private static Panel InputPanel(TextBox tb)
    {
        var panel = new Panel { Size = new Size(480, 52), BackColor = Color.White, Padding = new Padding(12, 12, 12, 12), BorderStyle = BorderStyle.FixedSingle };
        tb.Dock = DockStyle.Fill;
        panel.Controls.Add(tb);
        tb.GotFocus += (_, _) => panel.Padding = new Padding(12, 10, 12, 10);
        tb.LostFocus += (_, _) => panel.Padding = new Padding(12, 12, 12, 12);
        return panel;
    }

    private static Control CreateField(string labelText, Control field)
    {
        var label = new Label
        {
            Text = labelText,
            Font = AppTheme.Font(9.5f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            BackColor = Color.White
        };
        var container = new Panel { Size = new Size(480, field.Height + 36), BackColor = Color.White };
        label.Location = new Point(0, 0);
        field.Location = new Point(0, label.Bottom + 8);
        container.Controls.Add(label);
        container.Controls.Add(field);
        return container;
    }

    private void ResetPassword()
    {
        _message.Text = string.Empty;
        var email = _email.Text.Trim();
        var password = _newPassword.Text;
        var confirm = _confirmPassword.Text;

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Completează adresa de email.");
            AppTheme.MarkInvalid(_emailBox);
            return;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            ShowError("Parola trebuie să aibă cel puțin 8 caractere.");
            AppTheme.MarkInvalid(_newPasswordBox);
            return;
        }

        if (password != confirm)
        {
            ShowError("Parolele nu coincid.");
            AppTheme.MarkInvalid(_newPasswordBox);
            AppTheme.MarkInvalid(_confirmPasswordBox);
            return;
        }

        var user = _repo.GetByEmail(email);
        if (user == null)
        {
            ShowError("Niciun utilizator găsit pentru această adresă de email.");
            AppTheme.MarkInvalid(_emailBox);
            return;
        }

        if (!user.Activ)
        {
            ShowError("Acest cont este dezactivat. Contactează un administrator.");
            AppTheme.MarkInvalid(_emailBox);
            return;
        }

        if (!user.IsApproved)
        {
            ShowError("Contul nu este încă aprobat. Nu poți reseta parola până la aprobare.");
            AppTheme.MarkInvalid(_emailBox);
            return;
        }

        try
        {
            var updated = _repo.UpdateParola(user.Id, PasswordHelper.Hash(password));
            if (!updated)
            {
                ShowError("Nu am putut reseta parola. Încearcă din nou mai târziu.");
                return;
            }

            MessageBox.Show(this, "Parola a fost resetată cu succes. Te poți autentifica cu noua parolă.", "Resetare completă", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            ShowError("Eroare la resetare: " + ex.Message);
        }
    }

    private void ShowError(string message)
    {
        _message.Text = message;
        _body.PerformLayout();
    }
}
