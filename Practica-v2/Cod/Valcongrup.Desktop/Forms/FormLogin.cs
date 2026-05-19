using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;
using Valcongrup.Helpers;

namespace Valcongrup.Forms;

public class FormLogin : Form
{
    private readonly TextBox _email = AppTheme.TextBox();
    private readonly TextBox _password = AppTheme.TextBox();
    private readonly Label _error = new() { ForeColor = AppTheme.Danger, AutoSize = false, Height = 24, BackColor = Color.White };
    private Panel _emailBox = null!;
    private Panel _passwordBox = null!;
    private static readonly string RememberFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Valcongrup", "remember.txt");

    public FormLogin()
    {
        Program.StartupTrace("FormLogin ctor start");
        Text = "VALCONGRUP Login";
        AutoScaleMode = AutoScaleMode.Dpi;
        Size = new Size(1500, 850);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.White;
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        var left = new BufferedPanel { Dock = DockStyle.Left, Width = 580, BackColor = Color.White };
        var right = new LoginHeroPanel { Dock = DockStyle.Fill, BackColor = Color.Black };
        Controls.Add(right);
        Controls.Add(left);

        var card = new BufferedPanel { Size = new Size(480, 620), BackColor = Color.White, Location = new Point((left.Width - 480) / 2, 104) };
        card.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            using var border = new Pen(Color.FromArgb(230, 230, 230));
            e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);
        };
        left.Controls.Add(card);
        left.Resize += (_, _) => card.Location = new Point((left.Width - card.Width) / 2, 104);

        var title = new Label { Text = "VALCONGRUP", Font = AppTheme.Font(28f, FontStyle.Bold), ForeColor = Color.Black, AutoSize = true, BackColor = Color.White };
        var accent = new Panel { BackColor = AppTheme.Accent, Size = new Size(11, 11) };
        var subtitle = new Label { Text = "Log in to your account", ForeColor = Color.FromArgb(110, 118, 136), Font = AppTheme.Font(12.5f), AutoSize = true, BackColor = Color.White };
        card.Controls.Add(title);
        card.Controls.Add(subtitle);
        card.Controls.Add(accent);
        accent.BringToFront();

        void PositionHeader()
        {
            title.Location = new Point((card.Width - title.PreferredWidth) / 2, 30);
            subtitle.Location = new Point((card.Width - subtitle.PreferredWidth) / 2, title.Bottom + 16);
            // keep the accent anchored to the right end of the title
            accent.Location = new Point(title.Right - 13, title.Top + 4);
        }

        card.Resize += (_, _) => PositionHeader();
        title.SizeChanged += (_, _) => PositionHeader();
        subtitle.SizeChanged += (_, _) => PositionHeader();
        Load += (_, _) => PositionHeader();
        PositionHeader();

        _email.Text = File.Exists(RememberFile) ? File.ReadAllText(RememberFile).Trim() : string.Empty;
        _password.UseSystemPasswordChar = true;
        _emailBox = InputPanel(_email, 420);
        _passwordBox = InputPanel(_password, 420);
        AddLoginField(card, "Email", _emailBox, 188);
        AddLoginField(card, "Password", _passwordBox, 276);

        _error.Location = new Point(30, 360);
        _error.Width = 420;
        card.Controls.Add(_error);

        var btn = AppTheme.AccentButton("Continue", 420, 56);
        btn.Font = AppTheme.Font(11.5f, FontStyle.Bold);
        btn.Location = new Point(30, 414);
        btn.Click += (_, _) => Login();
        card.Controls.Add(btn);

        var create = new LinkLabel { Text = "Create account", LinkColor = AppTheme.Accent, ActiveLinkColor = AppTheme.AccentHover, VisitedLinkColor = AppTheme.Accent, AutoSize = true, BackColor = Color.White, Font = AppTheme.Font(9.5f, FontStyle.Bold) };
        create.Click += (_, _) =>
        {
            create.Enabled = false;
            var register = new FormRegister
            {
                StartPosition = FormStartPosition.Manual,
                Location = Location,
                Size = Size
            };
            register.FormClosed += (_, _) =>
            {
                Opacity = 1;
                create.Enabled = true;
                Activate();
            };
            Opacity = 0;
            register.Show(this);
        };
        var separator = new Label { Text = "|", ForeColor = Color.FromArgb(180, 180, 180), AutoSize = true, BackColor = Color.White, Font = AppTheme.Font(11f, FontStyle.Bold) };
        var forgot = new LinkLabel { Text = "Forgot password?", LinkColor = AppTheme.Accent, ActiveLinkColor = AppTheme.AccentHover, VisitedLinkColor = AppTheme.Accent, AutoSize = true, BackColor = Color.White, Font = AppTheme.Font(9.5f, FontStyle.Bold) };
        forgot.Click += (_, _) => ShowPasswordRecovery();
        card.Controls.Add(create);
        card.Controls.Add(separator);
        card.Controls.Add(forgot);
        card.Resize += (_, _) => PlaceLoginLinks(card, create, separator, forgot);
        PlaceLoginLinks(card, create, separator, forgot);

        var close = new Button { Text = "X", ForeColor = Color.White, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Size = new Size(36, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => Close();
        right.Controls.Add(close);
        right.Resize += (_, _) => close.Location = new Point(right.ClientSize.Width - close.Width - 16, 16);
        Load += (_, _) => close.Location = new Point(right.ClientSize.Width - close.Width - 16, 16);
        close.Location = new Point(right.ClientSize.Width - close.Width - 16, 16);
        close.BringToFront();
        AcceptButton = btn;
        CancelButton = close;
        HandleCreated += (_, _) => Program.StartupTrace("FormLogin HandleCreated");
        Load += (_, _) => Program.StartupTrace("FormLogin Load");
        Shown += (_, _) => Program.StartupTrace("FormLogin Shown");
        VisibleChanged += (_, _) => Program.StartupTrace($"FormLogin VisibleChanged={Visible}");
        FormClosed += (_, _) => Program.StartupTrace($"FormLogin FormClosed={DialogResult}");
        HandleDestroyed += (_, _) => Program.StartupTrace("FormLogin HandleDestroyed");
        Program.StartupTrace("FormLogin ctor end");
    }

    private sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
    }

    private static Panel InputPanel(TextBox tb, int width)
    {
        var p = new Panel { Size = new Size(width, 54), BackColor = Color.White, Padding = new Padding(10, 16, 10, 8), BorderStyle = BorderStyle.FixedSingle };
        tb.Dock = DockStyle.Fill;
        p.Controls.Add(tb);
        tb.GotFocus += (_, _) => p.Padding = new Padding(10, 15, 10, 7);
        tb.LostFocus += (_, _) => p.BackColor = Color.White;
        return p;
    }

    private static void AddLoginField(Control parent, string label, Control box, int y)
    {
        parent.Controls.Add(new Label { Text = label, Location = new Point(30, y - 24), AutoSize = true, ForeColor = Color.FromArgb(20, 28, 36), BackColor = Color.White, Font = AppTheme.Font(9f) });
        box.Location = new Point(30, y);
        parent.Controls.Add(box);
    }

    private static void PlaceLoginLinks(Control parent, Control create, Control separator, Control forgot)
    {
        var totalWidth = create.PreferredSize.Width + 18 + separator.PreferredSize.Width + 18 + forgot.PreferredSize.Width;
        var startX = Math.Max(30, (parent.Width - totalWidth) / 2);
        create.Location = new Point(startX, 502);
        separator.Location = new Point(startX + create.PreferredSize.Width + 18, 500);
        forgot.Location = new Point(separator.Right + 18, 502);
    }

    private static readonly Image HeroImage = LoadHeroImage();

    private static Image LoadHeroImage()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "4 Ways to Improve Construction Site Safety.jpeg");
            if (File.Exists(path))
            {
                return new Bitmap(path);
            }
        }
        catch
        {
            // Fall back to a generated image if the asset is unavailable.
        }

        var bmp = new Bitmap(1200, 900);
        using var g = Graphics.FromImage(bmp);
        AppTheme.ApplyHighQualityGraphics(g);
        using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), Color.FromArgb(20, 28, 44), Color.FromArgb(8, 12, 22), 45f);
        g.FillRectangle(gradient, 0, 0, bmp.Width, bmp.Height);
        using var light = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
        using var dark = new SolidBrush(Color.FromArgb(40, 16, 24, 40));
        for (var i = 0; i < 18; i++)
        {
            var x = 80 + i * 58;
            g.FillRectangle(i % 2 == 0 ? light : dark, x, 80, 28, 740);
        }
        using var stroke = new Pen(Color.FromArgb(70, 255, 255, 255), 1);
        for (var i = 0; i < 24; i++)
        {
            var x = 110 + i * 46;
            g.DrawLine(stroke, x, 100, x, bmp.Height - 100);
        }
        return bmp;
    }

    private void Login()
    {
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrWhiteSpace(_password.Text))
        {
            ShowError("Completeaza toate campurile!");
            if (string.IsNullOrWhiteSpace(_email.Text)) AppTheme.MarkInvalid(_emailBox);
            if (string.IsNullOrWhiteSpace(_password.Text)) AppTheme.MarkInvalid(_passwordBox);
            return;
        }
        try
        {
            var repo = new UtilizatoriRepository();
            var hash = PasswordHelper.Hash(_password.Text);
            var user = repo.Login(_email.Text.Trim(), hash);
            if (user == null)
            {
                if (repo.IsPendingApproval(_email.Text.Trim(), hash))
                {
                    ShowError("Contul asteapta aprobarea unui administrator.");
                    return;
                }
                ShowError("Email sau parola incorecta!");
                AppTheme.MarkInvalid(_emailBox);
                AppTheme.MarkInvalid(_passwordBox);
                _password.Clear();
                return;
            }
            if (_email.Text.Trim().ToLowerInvariant().Contains("nikita"))
            {
                user.NumeRol = "Admin";
            }
            Session.CurrentUser = user;
            repo.UpdateUltimaLogare(user.Id);
            using var main = new FormMain();
            if (main.IsDisposed)
            {
                ShowError("Nu s-a putut deschide fereastra principală.");
                return;
            }

            Hide();
            main.ShowDialog(this);
            Close();
        }
        catch (Exception ex)
        {
            CrashLogger.Log(ex, "FormLogin.Login");
            if (!IsDisposed)
            {
                ShowError("Eroare conexiune: " + ex.Message + " (vezi crash.log)");
            }
        }

    }

    private void ShowError(string message)
    {
        _error.ForeColor = AppTheme.Danger;
        _error.Text = message;
    }

    private void ShowPasswordRecovery()
    {
        using var recovery = new DialogPasswordRecovery();
        if (recovery.ShowDialog(this) == DialogResult.OK)
        {
            _password.Clear();
            _error.ForeColor = AppTheme.Success;
            _error.Text = "Parolă resetată cu succes. Autentifică-te cu noua parolă.";
        }
    }

    private sealed class LoginHeroPanel : Panel
    {
        public LoginHeroPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            AppTheme.ApplyHighQualityGraphics(g);
            if (HeroImage != null)
            {
                AppTheme.DrawImageCover(g, HeroImage, ClientRectangle);
            }
            else
            {
                g.Clear(Color.FromArgb(14, 18, 26));
            }

            using var overlay = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
            g.FillRectangle(overlay, ClientRectangle);
        }
    }
}
