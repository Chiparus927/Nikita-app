using System.Text.RegularExpressions;
using Valcongrup.Data;
using Valcongrup.Helpers;

namespace Valcongrup.UserControls;

public class UcSetari : UserControl
{
    private readonly TextBox _nume = AppTheme.TextBox();
    private readonly TextBox _prenume = AppTheme.TextBox();
    private readonly TextBox _telefon = AppTheme.TextBox();
    private readonly TextBox _veche = AppTheme.TextBox();
    private readonly TextBox _noua = AppTheme.TextBox();
    private readonly TextBox _confirma = AppTheme.TextBox();
    private readonly Label _msg = UiFactory.Label("", 9f, AppTheme.Success);
    private readonly ThemeSwitch _themeSwitch = new();
    private readonly RoundedPanel _body;
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9\s().-]{7,20}$", RegexOptions.Compiled);

    public UcSetari()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);
        _veche.UseSystemPasswordChar = _noua.UseSystemPasswordChar = _confirma.UseSystemPasswordChar = true;

        var u = Session.CurrentUser!;
        _nume.Text = u.Nume;
        _prenume.Text = u.Prenume;
        _telefon.Text = u.Telefon;

        _body = new RoundedPanel
        {
            Height = 650,
            Width = 540,
            BackColor = AppTheme.Card,
            Padding = new Padding(28),
            Radius = 12,
            Anchor = AnchorStyles.Top
        };

        var save = AppTheme.AccentButton("Salveaza", 480, 46);
        save.Dock = DockStyle.Top;
        save.Click += (_, _) => SaveProfile();

        var pass = AppTheme.AccentButton("Schimba parola", 480, 46);
        pass.Dock = DockStyle.Top;
        pass.Click += (_, _) => ChangePassword();

        _themeSwitch.Checked = AppTheme.IsDarkMode;
        _themeSwitch.ThemeChanged += (_, dark) =>
        {
            AppTheme.SetMode(dark);
            ApplyTheme();
        };

        _body.Controls.AddRange(new Control[]
        {
            _msg,
            pass,
            UiFactory.Field("Confirma parola", _confirma),
            UiFactory.Field("Parola noua", _noua),
            UiFactory.Field("Parola veche", _veche),
            save,
            UiFactory.Field("Telefon", _telefon),
            UiFactory.Field("Prenume", _prenume),
            UiFactory.Field("Nume", _nume),
            BuildThemeRow()
        });

        _body.Location = new Point(260, 170);
        Resize += (_, _) => _body.Location = new Point(Math.Max(48, (ClientSize.Width - _body.Width) / 2), 178);
        Controls.Add(_body);
        Controls.Add(ModernUi.PageHeader("Setari", $"Cont: {u.Email} | Rol: {u.NumeRol}"));
        ApplyTheme();
    }

    private Panel BuildThemeRow()
    {
        var row = new Panel { Height = 78, Dock = DockStyle.Top, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 12) };
        var label = UiFactory.Label("Tema aplicatie", 9.25f, AppTheme.TextSecond);
        label.Location = new Point(0, 8);
        var hint = UiFactory.Label("White / Black mode", 8.5f, AppTheme.TextSecond);
        hint.Location = new Point(0, 34);
        _themeSwitch.Location = new Point(360, 11);
        row.Controls.AddRange(new Control[] { label, hint, _themeSwitch });
        return row;
    }

    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(_nume.Text) || string.IsNullOrWhiteSpace(_prenume.Text))
        {
            ShowMsg("Numele si prenumele sunt obligatorii.", true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_telefon.Text) && !PhoneRegex.IsMatch(_telefon.Text.Trim()))
        {
            ShowMsg("Telefon invalid. Foloseste cifre si optional +, spatii sau cratime.", true);
            return;
        }

        UiFactory.Try(() =>
        {
            var u = Session.CurrentUser!;
            u.Nume = _nume.Text.Trim();
            u.Prenume = _prenume.Text.Trim();
            u.Telefon = _telefon.Text.Trim();
            new UtilizatoriRepository().Update(u);
            new JurnalRepository().Log(u.Id, null, "A actualizat informatiile personale", "Utilizator", u.Id);
            ShowMsg("Profil salvat.", false);
        });
    }

    private void ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(_veche.Text) || string.IsNullOrWhiteSpace(_noua.Text))
        {
            ShowMsg("Completeaza parola veche si parola noua.", true);
            return;
        }

        if (_noua.Text != _confirma.Text)
        {
            ShowMsg("Parolele nu coincid.", true);
            return;
        }

        if (_noua.Text.Length < 8)
        {
            ShowMsg("Parola noua trebuie sa aiba minim 8 caractere.", true);
            return;
        }

        UiFactory.Try(() =>
        {
            var ok = new UtilizatoriRepository().UpdateParola(
                Session.CurrentUser!.Id,
                PasswordHelper.Hash(_veche.Text),
                PasswordHelper.Hash(_noua.Text));
            ShowMsg(ok ? "Parola schimbata cu succes." : "Parola veche incorecta.", !ok);
        });
    }

    private void ShowMsg(string text, bool error)
    {
        _msg.Text = text;
        _msg.ForeColor = error ? AppTheme.Danger : AppTheme.Success;
    }

    private void ApplyTheme()
    {
        BackColor = AppTheme.Shell;
        _body.BackColor = AppTheme.Card;
        _body.BorderColor = AppTheme.Border;
        ApplyThemeToControls(this);
        FindForm()?.Invalidate(true);
        Invalidate(true);
    }

    private static void ApplyThemeToControls(Control root)
    {
        foreach (Control c in root.Controls)
        {
            switch (c)
            {
                case Label lbl when lbl.ForeColor != AppTheme.Danger && lbl.ForeColor != AppTheme.Success:
                    lbl.ForeColor = AppTheme.TextSecond;
                    break;
                case TextBox tb:
                    tb.BackColor = AppTheme.IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.White;
                    tb.ForeColor = AppTheme.TextPrimary;
                    break;
                case RoundedPanel panel:
                    panel.BackColor = AppTheme.Card;
                    panel.BorderColor = AppTheme.Border;
                    break;
                case Panel p:
                    p.BackColor = Color.Transparent;
                    break;
            }

            ApplyThemeToControls(c);
        }
    }

    private sealed class ThemeSwitch : Control
    {
        private bool _checked;
        private bool _hover;

        public event EventHandler<bool>? ThemeChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                Invalidate();
            }
        }

        public ThemeSwitch()
        {
            Size = new Size(116, 48);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left)
                return;

            Checked = !Checked;
            ThemeChanged?.Invoke(this, Checked);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 8 || Height < 8)
                return;

            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            var track = new Rectangle(0, 0, Width - 1, Height - 1);
            using var trackPath = AppTheme.RoundedPath(track, 18);
            using var bg = new SolidBrush(Checked ? Color.FromArgb(31, 41, 55) : Color.FromArgb(229, 231, 235));
            using var border = new Pen(_hover ? AppTheme.Accent : AppTheme.Border, 1.5f);
            e.Graphics.FillPath(bg, trackPath);
            e.Graphics.DrawPath(border, trackPath);

            var knob = Checked ? new Rectangle(60, 5, 50, 38) : new Rectangle(6, 5, 50, 38);
            using var knobPath = AppTheme.RoundedPath(knob, 14);
            using var knobFill = new SolidBrush(Color.White);
            e.Graphics.FillPath(knobFill, knobPath);

            using var moonPen = new Pen(Checked ? Color.FromArgb(203, 213, 225) : Color.FromArgb(95, 95, 95), 2.2f);
            e.Graphics.DrawArc(moonPen, 22, 13, 20, 20, 90, 220);

            using var sunFill = new SolidBrush(Checked ? AppTheme.Accent : Color.FromArgb(80, 80, 80));
            e.Graphics.FillEllipse(sunFill, knob.X + 17, knob.Y + 11, 16, 16);
            using var sunRing = new Pen(Color.FromArgb(150, 80, 80, 80), 1f);
            e.Graphics.DrawEllipse(sunRing, knob.X + 10, knob.Y + 5, 30, 28);
        }
    }
}
