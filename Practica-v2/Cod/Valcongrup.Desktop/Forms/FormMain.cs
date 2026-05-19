using System.Data;
using System.Drawing.Drawing2D;
using Valcongrup.Forms.Dialogs;
using Valcongrup.UserControls;

namespace Valcongrup.Forms;

public class FormMain : Form
{
    private static readonly Color NavOrange = Color.FromArgb(255, 122, 0);
    private static readonly Color NavOrangeHover = Color.FromArgb(234, 88, 12);
    private static readonly Color NavInk = Color.FromArgb(14, 23, 42);
    private static readonly Color NavMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color NavChrome = Color.FromArgb(248, 250, 252);
    private static readonly Color NavWarm = Color.FromArgb(255, 247, 237);
    private static readonly Color NavBorder = Color.FromArgb(226, 232, 240);

    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = AppTheme.Shell };
    private readonly List<SidebarNavItem> _navItems = new();
    private SidebarNavItem? _firstNav;
    private Func<UserControl>? _firstModuleFactory;
    private Func<UserControl>? _activeModuleFactory;
    private Label? _statusChip;
    private System.Windows.Forms.Timer? _clockTimer;

    public FormMain()
    {
        try
        {
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Sesiunea a expirat. Autentifica-te din nou.", "VALCONGRUP");
                Close();
                return;
            }

            Text = "VALCONGRUP";
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(1440, 900);
            WindowState = FormWindowState.Maximized;
            BackColor = AppTheme.Shell;
            Font = AppTheme.Font();

            Controls.Add(_content);
            Controls.Add(BuildTopbar());
            Controls.Add(BuildSidebar());

            AppTheme.ThemeChanged += OnThemeChanged;
            FormClosed += (_, _) => AppTheme.ThemeChanged -= OnThemeChanged;
            BuildClock();
            SetActiveNav(_firstNav!);
            LoadModule((_firstModuleFactory ?? (() => new UcDashboard()))());
            ApplyCurrentTheme();
        }
        catch (Exception ex)
        {
            CrashLogger.Log(ex, "FormMain.ctor");
            throw;
        }
    }

    private void BuildClock()
    {
        _statusChip = new Label
        {
            AutoSize = true,
            BackColor = AppTheme.IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(255, 250, 245),
            ForeColor = AppTheme.TextOnLightSecondary,
            Font = AppTheme.Font(8.75f, FontStyle.Bold),
            Padding = new Padding(12, 7, 12, 7)
        };
        AppTheme.ApplyRoundedRegion(_statusChip, 12);
        _content.Controls.Add(_statusChip);

        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) =>
        {
            if (_statusChip == null || _content.IsDisposed)
                return;

            _statusChip.Text = DateTime.Now.ToString("dd MMM yyyy   HH:mm");
            _statusChip.BackColor = AppTheme.IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(255, 250, 245);
            _statusChip.ForeColor = AppTheme.TextOnLightSecondary;
            AppTheme.ApplyRoundedRegion(_statusChip, 12);
            _statusChip.Location = new Point(
                _content.ClientSize.Width - _statusChip.Width - 28,
                _content.ClientSize.Height - _statusChip.Height - 20);
            _statusChip.BringToFront();
        };
        _clockTimer.Start();
    }

    private Panel BuildTopbar()
    {
        var topbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 94,
            BackColor = AppTheme.Card,
            Padding = new Padding(28, 0, 32, 0)
        };
        topbar.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            if (!AppTheme.IsDarkMode)
            {
                using var pen = new Pen(AppTheme.ShellBorder, 1);
                e.Graphics.DrawLine(pen, 0, topbar.Height - 1, topbar.Width, topbar.Height - 1);
            }
        };

        var searchPanel = new RoundedPanel
        {
            Width = 560,
            Height = 52,
            Radius = 16,
            BackColor = AppTheme.Card,
            Padding = new Padding(50, 0, 14, 0),
            BorderColor = AppTheme.Border,
            Location = new Point(48, 20)
        };
        searchPanel.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            searchPanel.BackColor = AppTheme.Card;
            searchPanel.BorderColor = AppTheme.IsDarkMode ? Color.FromArgb(71, 85, 105) : AppTheme.Border;
            using var pen = new Pen(AppTheme.TextOnLightSecondary, 1.8f);
            e.Graphics.DrawEllipse(pen, 16, 17, 14, 14);
            e.Graphics.DrawLine(pen, 27, 29, 34, 36);
        };

        const string placeholder = "Cauta proiecte, sarcini sau documente...";
        var txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = AppTheme.Font(11f),
            ForeColor = AppTheme.TextOnLightSecondary,
            BackColor = AppTheme.Card,
            Text = placeholder,
            Location = new Point(50, 16),
            Width = searchPanel.Width - 68,
            Height = 24,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        searchPanel.Resize += (_, _) =>
        {
            txtSearch.Location = new Point(50, (searchPanel.Height - txtSearch.Height) / 2 + 1);
            txtSearch.Width = searchPanel.Width - 68;
        };
        txtSearch.Location = new Point(50, (searchPanel.Height - txtSearch.Height) / 2 + 1);
        txtSearch.GotFocus += (_, _) =>
        {
            if (txtSearch.Text == placeholder)
            {
                txtSearch.Text = string.Empty;
                txtSearch.ForeColor = AppTheme.TextOnLight;
            }
        };
        txtSearch.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = placeholder;
                txtSearch.ForeColor = AppTheme.TextOnLightSecondary;
            }
        };
        txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyword) || keyword == placeholder)
                return;

            Task.Run(() =>
            {
                try
                {
                    var results = new Valcongrup.Data.ProiecteRepository().Search(keyword);
                    void Show()
                    {
                        SetActiveNav(_firstNav!);
                        LoadModule(new UcSearchResults(keyword, results));
                    }

                    if (InvokeRequired)
                        Invoke(Show);
                    else
                        Show();
                }
                catch (Exception ex)
                {
                    void ShowError() => MessageBox.Show(ex.Message, "Cautare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (InvokeRequired)
                        Invoke(ShowError);
                    else
                        ShowError();
                }
            });
        };
        searchPanel.Controls.Add(txtSearch);

        var bell = new NotificationIcon
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 274, 24),
            Size = new Size(40, 40),
            Cursor = Cursors.Hand
        };
        bell.Click += (_, _) => ShowNotifications(bell);

        var add = AppTheme.AccentButton("Proiect Nou", 168, 46);
        add.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        add.Location = new Point(Width - 212, 22);
        add.Visible = Session.IsManagerOrAdmin();
        add.Click += (_, _) =>
        {
            if (!Session.IsManagerOrAdmin())
                return;

            using var dlg = new DialogProiect();
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            UiFactory.Try(() =>
            {
                var id = new Valcongrup.Data.ProiecteRepository().Insert(dlg.Proiect);
                new Valcongrup.Data.JurnalRepository().Log(
                    Session.CurrentUser!.Id,
                    id,
                    $"A adaugat proiectul '{dlg.Proiect.Nume}'",
                    "Proiect",
                    id);
            });
        };

        void PlaceTopbarActions()
        {
            if (add.Visible)
            {
                add.Location = new Point(topbar.Width - 212, 22);
                bell.Location = new Point(topbar.Width - 274, 24);
            }
            else
            {
                bell.Location = new Point(topbar.Width - 62, 24);
            }
        }

        topbar.Controls.AddRange(new Control[] { searchPanel, bell, add });
        topbar.Resize += (_, _) => PlaceTopbarActions();
        PlaceTopbarActions();
        return topbar;
    }

    private void ShowNotifications(Control bell)
    {
        var dropDown = new ToolStripDropDown { Padding = Padding.Empty, AutoClose = true };
        var panel = new RoundedPanel
        {
            Width = 380,
            Height = 360,
            Radius = 16,
            BackColor = AppTheme.Card,
            Padding = new Padding(18),
            BorderColor = AppTheme.ShellBorder
        };

        var title = ModernUi.Text("Notificari", 13f, AppTheme.TextOnLight, FontStyle.Bold);
        title.Location = new Point(18, 16);
        panel.Controls.Add(title);

        var list = new FlowLayoutPanel
        {
            Location = new Point(14, 54),
            Size = new Size(352, 286),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White
        };
        panel.Controls.Add(list);

        try
        {
            var table = new Valcongrup.Data.JurnalRepository().GetNotifications(5);
            if (table.Rows.Count == 0)
            {
                list.Controls.Add(new Label
                {
                    Text = "Nu exista notificari recente.",
                    Width = 330,
                    Height = 34,
                    Font = AppTheme.Font(10f),
                    ForeColor = AppTheme.TextOnLightSecondary
                });
            }

            foreach (DataRow row in table.Rows)
            {
                var item = new Panel { Width = 330, Height = 66, BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
                item.Paint += (_, e) =>
                {
                    AppTheme.ApplyHighQualityGraphics(e.Graphics);
                    using var path = AppTheme.RoundedPath(new Rectangle(0, 0, item.Width - 1, item.Height - 1), 12);
                    using var brush = new SolidBrush(AppTheme.CardAlt);
                    using var pen = new Pen(AppTheme.ShellBorder);
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                };

                var text = new Label
                {
                    Text = Convert.ToString(row["actiune"]) ?? string.Empty,
                    Font = AppTheme.Font(9.5f, FontStyle.Bold),
                    ForeColor = AppTheme.TextOnLight,
                    Location = new Point(14, 10),
                    Size = new Size(300, 28),
                    BackColor = Color.Transparent
                };
                var when = Convert.ToDateTime(row["creat_la"]).ToString("dd MMM, HH:mm");
                var time = ModernUi.Text(when, 8.5f, AppTheme.TextOnLightSecondary);
                time.Location = new Point(14, 40);
                item.Controls.AddRange(new Control[] { text, time });
                list.Controls.Add(item);
            }
        }
        catch (Exception ex)
        {
            list.Controls.Add(new Label
            {
                Text = "Notificarile nu au putut fi incarcate: " + ex.Message,
                Width = 330,
                Height = 70,
                Font = AppTheme.Font(9.5f),
                ForeColor = AppTheme.Danger
            });
        }

        dropDown.Items.Add(new ToolStripControlHost(panel)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = panel.Size
        });
        dropDown.Show(bell, new Point(bell.Width - panel.Width, bell.Height + 12));
    }

    private Panel BuildSidebar()
    {
        var side = new PremiumHeaderPanel
        {
            Dock = DockStyle.Top,
            Height = 104,
            BackColor = Color.Transparent,
            Padding = new Padding(28, 0, 28, 12)
        };

        var brandCard = new BrandBridgePanel
        {
            Width = 318,
            Height = 66,
            BackColor = Color.Transparent
        };
        var logo = new BrandLogo { Size = new Size(46, 46), Location = new Point(12, 8) };
        var brand = ModernUi.Text("ValconGrup", 16f, NavInk, FontStyle.Bold);
        brand.Location = new Point(74, 8);
        var tag = ModernUi.Text("Constructii si Management", 9.25f, Color.FromArgb(92, 107, 128), FontStyle.Bold);
        tag.Location = new Point(75, 38);
        brandCard.Controls.AddRange(new Control[] { logo, brand, tag });

        var navShell = new Panel
        {
            Name = "NavShell",
            Height = 48,
            BackColor = Color.White,
            Padding = new Padding(0)
        };
        side.Controls.Add(navShell);
        side.Controls.Add(brandCard);

        var navStrip = new FlowLayoutPanel
        {
            Name = "NavStrip",
            Height = 46,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        navShell.Controls.Add(navStrip);

        var y = 0;
        if (Session.IsSubcontractor())
        {
            AddNav(side, ref y, "P", "Proiecte", () => new UcProiecte());
            AddNav(side, ref y, "C", "Calendar", () => new UcCalendar());
        }
        else
        {
            AddNav(side, ref y, "D", "Dashboard", () => new UcDashboard());
            AddNav(side, ref y, "P", "Proiecte", () => new UcProiecte());
            AddNav(side, ref y, "S", "Sarcini", () => new UcSarcini());
            AddNav(side, ref y, "U", "Utilizatori", () => new UcEchipa());
            AddNav(side, ref y, "R", "Rapoarte", () => new UcRapoarte());
            AddNav(side, ref y, "F", "Documente", () => new UcDocumente());
            AddNav(side, ref y, "C", "Calendar", () => new UcCalendar());
            if (Session.IsAdmin())
                AddNav(side, ref y, "A", "Administrare", () => new UcUserManagement());
        }

        if (!Session.IsSubcontractor())
        {
            AddNav(side, ref y, "G", "Setari", () => new UcSetari());
        }

        var user = new ProfileCard { Width = 306, Height = 56, BackColor = Color.Transparent };
        user.Paint += (_, e) =>
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
        };

        var avatar = new InitialAvatar($"{Session.CurrentUser!.Prenume.FirstOrDefault()}{Session.CurrentUser.Nume.FirstOrDefault()}")
        {
            Size = new Size(42, 42),
            Location = new Point(14, 7)
        };
        var name = ModernUi.Text($"{Session.CurrentUser.Prenume} {Session.CurrentUser.Nume}", 9.5f, NavInk, FontStyle.Bold);
        name.Location = new Point(72, 9);
        var role = ModernUi.Text(
            string.IsNullOrWhiteSpace(Session.CurrentUser.NumeRol) ? "Admin" : Session.CurrentUser.NumeRol,
            8f,
            NavMuted);
        role.Location = new Point(72, 31);

        var logout = new PremiumLogoutButton
        {
            Text = "Iesire",
            ForeColor = Color.White,
            BackColor = NavOrange,
            Size = new Size(86, 32),
            Location = new Point(208, 12),
            Cursor = Cursors.Hand,
            Font = AppTheme.Font(8.75f, FontStyle.Bold)
        };
        logout.Click += (_, _) =>
        {
            Session.CurrentUser = null;
            Hide();
            new FormLogin().ShowDialog();
            Close();
        };

        user.Controls.AddRange(new Control[] { avatar, name, role, logout });
        side.Controls.Add(user);

        void LayoutTopNav()
        {
            brandCard.Location = new Point(34, 18);
            brandCard.BringToFront();

            user.Location = new Point(side.ClientSize.Width - user.Width - 34, 19);

            var navLeft = brandCard.Right + 34;
            var navRight = user.Left - 34;
            navShell.SetBounds(navLeft, 24, Math.Max(820, navRight - navLeft), 48);
            navStrip.SetBounds(0, 0, navShell.Width, 46);

            CompactNavItems(navStrip.Width);
        }

        side.Resize += (_, _) => LayoutTopNav();
        side.HandleCreated += (_, _) => LayoutTopNav();
        LayoutTopNav();
        return side;
    }

    private void AddNav(Panel side, ref int y, string iconCode, string text, Func<UserControl> factory)
    {
        SidebarNavItem item = null!;
        item = new SidebarNavItem(iconCode, text, () =>
        {
            SetActiveNav(item);
            _activeModuleFactory = factory;
            LoadModule(factory());
        });
        item.Size = new Size(GetNavWidth(text, false), 40);

        FlowLayoutPanel? strip = null;
        if (side.Controls["NavShell"] is Control shell)
            strip = shell.Controls["NavStrip"] as FlowLayoutPanel;
        strip ??= side.Controls["NavStrip"] as FlowLayoutPanel;

        if (strip != null)
        {
            item.Margin = new Padding(0, 0, 22, 0);
            strip.Controls.Add(item);
        }
        else
        {
            item.Location = new Point(12, y);
            side.Controls.Add(item);
        }

        _navItems.Add(item);
        _firstNav ??= item;
        _firstModuleFactory ??= factory;
        _activeModuleFactory ??= factory;
        y += 64;
    }

    private static int GetNavWidth(string text, bool compact)
    {
        var textWidth = TextRenderer.MeasureText(text, AppTheme.Font(9.25f, FontStyle.Bold)).Width;
        if (compact)
            return Math.Max(76, textWidth + 18);

        return text switch
        {
            "Dashboard" => 104,
            "Proiecte" => 94,
            "Sarcini" => 86,
            "Utilizatori" => 106,
            "Rapoarte" => 98,
            "Documente" => 118,
            "Calendar" => 100,
            "Administrare" => 126,
            "Setari" => 76,
            _ => Math.Max(76, textWidth + 22)
        };
    }

    private void CompactNavItems(int availableWidth)
    {
        var total = _navItems.Sum(i => GetNavWidth(i.LabelText, false) + i.Margin.Horizontal);
        var compact = total > availableWidth;

        foreach (var item in _navItems)
        {
            item.CompactMode = compact;
            item.Width = GetNavWidth(item.LabelText, compact);
        }
    }

    private void SetActiveNav(SidebarNavItem active)
    {
        foreach (var n in _navItems)
            n.SetActive(false);
        active.SetActive(true);
    }

    private void LoadModule(UserControl uc)
    {
        _content.Controls.Clear();
        uc.Dock = DockStyle.Fill;
        _content.Controls.Add(uc);
        if (_statusChip != null)
            _content.Controls.Add(_statusChip);
        uc.BringToFront();
        _statusChip?.BringToFront();
    }

    private sealed class PremiumHeaderPanel : Panel
    {
        public PremiumHeaderPanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2)
                return;

            AppTheme.ApplyHighQualityGraphics(e.Graphics);

            e.Graphics.Clear(AppTheme.Card);
            TextRenderer.DrawText(
                e.Graphics,
                string.Empty,
                AppTheme.Font(7.75f, FontStyle.Bold),
                new Rectangle(0, 0, Math.Max(1, Width - 34), 20),
                Color.White,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

            using var shadow = new LinearGradientBrush(
                new Rectangle(0, Math.Max(0, Height - 14), Math.Max(1, Width), 14),
                Color.FromArgb(12, 15, 23, 42),
                Color.Transparent,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(shadow, 0, Math.Max(0, Height - 14), Width, 14);

            using var pen = new Pen(NavBorder, 1);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }

    private sealed class ProfileCard : Panel
    {
        public ProfileCard()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 8 || Height < 8)
                return;

            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            var shadowRect = new Rectangle(2, 4, Math.Max(1, Width - 4), Math.Max(1, Height - 7));
            using (var shadowPath = AppTheme.RoundedPath(shadowRect, 24))
            using (var shadow = new SolidBrush(Color.FromArgb(12, 15, 23, 42)))
                e.Graphics.FillPath(shadow, shadowPath);

            var rect = new Rectangle(0, 0, Math.Max(1, Width - 3), Math.Max(1, Height - 7));
            using var path = AppTheme.RoundedPath(rect, 24);
            using var fill = new SolidBrush(AppTheme.Card);
            using var border = new Pen(AppTheme.Border, 1);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }
    }

    private sealed class BrandBridgePanel : Panel
    {
        public BrandBridgePanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Intentionally empty: the company logo should not have a decorative block behind it.
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyCurrentTheme();

    private void ApplyCurrentTheme()
    {
        BackColor = AppTheme.Shell;
        _content.BackColor = AppTheme.Shell;
        if (_activeModuleFactory != null && _content.Controls.Count > 0)
            LoadModule(_activeModuleFactory());
        AppTheme.ApplyToTree(this);
        Invalidate(true);
    }

    private sealed class PremiumLogoutButton : Control
    {
        private bool _hover;

        public PremiumLogoutButton()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
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

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (Width < 8 || Height < 8)
                return;

            AppTheme.ApplyHighQualityGraphics(pevent.Graphics);
            using (var clear = new SolidBrush(AppTheme.Card))
                pevent.Graphics.FillRectangle(clear, ClientRectangle);
            var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 3));
            using var path = AppTheme.RoundedPath(rect, rect.Height / 2);
            using var brush = new LinearGradientBrush(
                rect,
                _hover ? NavOrangeHover : NavOrange,
                _hover ? Color.FromArgb(255, 132, 22) : Color.FromArgb(255, 149, 48),
                LinearGradientMode.Horizontal);
            pevent.Graphics.FillPath(brush, path);
            using var border = new Pen(_hover ? Color.FromArgb(255, 160, 64) : NavOrange, 1f);
            pevent.Graphics.DrawPath(border, path);

            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                Font,
                rect,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private sealed class SidebarNavItem : Panel
    {
        private readonly string _code;
        private readonly string _label;
        private readonly Action _activate;
        private readonly System.Windows.Forms.Timer _animationTimer;
        private bool _active;
        private bool _hover;
        private float _hoverProgress;
        private float _activeProgress;
        private bool _compactMode;

        public SidebarNavItem(string code, string label, Action activate)
        {
            _code = code;
            _label = label;
            _activate = activate;
            Size = new Size(276, 54);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            _animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _animationTimer.Tick += (_, _) => Animate();
        }

        public string Code => _code;
        public string LabelText => _label;
        public bool CompactMode
        {
            get => _compactMode;
            set
            {
                if (_compactMode == value)
                    return;
                _compactMode = value;
                Invalidate();
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            StartAnimation();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            StartAnimation();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            StartAnimation();
        }

        private void StartAnimation()
        {
            if (!_animationTimer.Enabled)
                _animationTimer.Start();
        }

        private void Animate()
        {
            var hoverTarget = _hover ? 1f : 0f;
            var activeTarget = _active ? 1f : 0f;
            _hoverProgress = Step(_hoverProgress, hoverTarget, 0.16f);
            _activeProgress = Step(_activeProgress, activeTarget, 0.18f);
            Invalidate();

            if (Math.Abs(_hoverProgress - hoverTarget) < 0.01f &&
                Math.Abs(_activeProgress - activeTarget) < 0.01f)
            {
                _hoverProgress = hoverTarget;
                _activeProgress = activeTarget;
                _animationTimer.Stop();
            }
        }

        private static float Step(float current, float target, float amount)
        {
            if (current < target)
                return Math.Min(target, current + amount);
            return Math.Max(target, current - amount);
        }

        private static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            return Color.FromArgb(
                (int)(from.A + (to.A - from.A) * amount),
                (int)(from.R + (to.R - from.R) * amount),
                (int)(from.G + (to.G - from.G) * amount),
                (int)(from.B + (to.B - from.B) * amount));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 8 || Height < 8)
                return;

            var g = e.Graphics;
            AppTheme.ApplyHighQualityGraphics(g);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            var emphasis = Math.Max(_activeProgress, _hoverProgress);
            var textColor = Blend(AppTheme.TextOnLight, NavOrange, emphasis);
            if (_hoverProgress > 0.02f)
            {
                using var hoverLine = new Pen(Color.FromArgb((int)(105 * _hoverProgress), NavOrange), 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawLine(hoverLine, 12, Height - 8, Width - 12, Height - 8);
            }

            if (_activeProgress > 0.02f)
            {
                using var activeLine = new Pen(Color.FromArgb((int)(255 * _activeProgress), NavOrange), 3f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawLine(activeLine, 10, Height - 7, Width - 10, Height - 7);
            }

            TextRenderer.DrawText(
                g,
                _label,
                AppTheme.Font(9.75f, _active ? FontStyle.Bold : FontStyle.Regular),
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
                _activate();
        }
    }

    private static void DrawNavGlyph(Graphics g, string code, int x, int y, Color color)
    {
        AppTheme.ApplyHighQualityGraphics(g);
        using var pen = new Pen(color, 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var r = new Rectangle(x, y, 24, 24);
        if (code == "D")
        {
            g.DrawRectangle(pen, r.X + 3, r.Y + 3, 7, 7);
            g.DrawRectangle(pen, r.X + 14, r.Y + 3, 7, 7);
            g.DrawRectangle(pen, r.X + 3, r.Y + 14, 7, 7);
            g.DrawRectangle(pen, r.X + 14, r.Y + 14, 7, 7);
        }
        else if (code == "P")
        {
            g.DrawRectangle(pen, r.X + 3, r.Y + 7, 18, 12);
            g.DrawLine(pen, r.X + 5, r.Y + 7, r.X + 8, r.Y + 3);
            g.DrawLine(pen, r.X + 8, r.Y + 3, r.X + 16, r.Y + 3);
            g.DrawLine(pen, r.X + 16, r.Y + 3, r.X + 20, r.Y + 7);
        }
        else if (code == "S")
        {
            g.DrawRectangle(pen, r.X + 4, r.Y + 3, 16, 18);
            g.DrawLine(pen, r.X + 8, r.Y + 12, r.X + 11, r.Y + 15);
            g.DrawLine(pen, r.X + 11, r.Y + 15, r.X + 17, r.Y + 8);
        }
        else if (code == "U")
        {
            g.DrawEllipse(pen, r.X + 4, r.Y + 4, 6, 6);
            g.DrawEllipse(pen, r.X + 14, r.Y + 4, 6, 6);
            g.DrawArc(pen, r.X + 1, r.Y + 12, 12, 9, 180, 180);
            g.DrawArc(pen, r.X + 11, r.Y + 12, 12, 9, 180, 180);
        }
        else if (code == "R")
        {
            g.DrawLine(pen, r.X + 5, r.Y + 19, r.X + 5, r.Y + 12);
            g.DrawLine(pen, r.X + 12, r.Y + 19, r.X + 12, r.Y + 7);
            g.DrawLine(pen, r.X + 19, r.Y + 19, r.X + 19, r.Y + 10);
            g.DrawLine(pen, r.X + 3, r.Y + 20, r.X + 21, r.Y + 20);
        }
        else if (code == "F")
        {
            g.DrawRectangle(pen, r.X + 4, r.Y + 5, 16, 15);
            g.DrawLine(pen, r.X + 4, r.Y + 9, r.X + 10, r.Y + 9);
            g.DrawLine(pen, r.X + 10, r.Y + 5, r.X + 13, r.Y + 9);
        }
        else if (code == "C")
        {
            g.DrawRectangle(pen, r.X + 4, r.Y + 5, 16, 15);
            g.DrawLine(pen, r.X + 4, r.Y + 10, r.X + 20, r.Y + 10);
            g.DrawLine(pen, r.X + 8, r.Y + 3, r.X + 8, r.Y + 7);
            g.DrawLine(pen, r.X + 16, r.Y + 3, r.X + 16, r.Y + 7);
        }
        else if (code == "A")
        {
            g.DrawEllipse(pen, r.X + 7, r.Y + 3, 10, 10);
            g.DrawArc(pen, r.X + 4, r.Y + 13, 16, 10, 180, 180);
            g.DrawLine(pen, r.X + 18, r.Y + 5, r.X + 22, r.Y + 5);
            g.DrawLine(pen, r.X + 20, r.Y + 3, r.X + 20, r.Y + 7);
        }
        else
        {
            g.DrawEllipse(pen, r.X + 4, r.Y + 4, 16, 16);
            g.DrawLine(pen, r.X + 12, r.Y + 8, r.X + 12, r.Y + 16);
            g.DrawLine(pen, r.X + 8, r.Y + 12, r.X + 16, r.Y + 12);
        }
    }

    private sealed class NotificationIcon : Control
    {
        public NotificationIcon()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 8 || Height < 8)
                return;

            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Shell);

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using var path = AppTheme.RoundedPath(rect, 14);
            using var fill = new SolidBrush(AppTheme.IsDarkMode ? Color.FromArgb(17, 24, 39) : AppTheme.Card);
            using var border = new Pen(AppTheme.IsDarkMode ? Color.FromArgb(71, 85, 105) : AppTheme.Border, 1);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            using var pen = new Pen(AppTheme.TextOnLightSecondary, 1.8f);
            e.Graphics.DrawArc(pen, 11, 10, 16, 16, 190, 160);
            e.Graphics.DrawLine(pen, 11, 24, 27, 24);
            e.Graphics.DrawArc(pen, 15, 24, 8, 6, 0, 180);

            using var dot = new SolidBrush(AppTheme.Accent);
            e.Graphics.FillEllipse(dot, Width - 12, 6, 7, 7);
        }
    }

    private sealed class BrandLogo : Control
    {
        private static Image? _logo;

        public BrandLogo()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
                Region = new Region(AppTheme.RoundedPath(new Rectangle(0, 0, Width, Height), 14));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = AppTheme.RoundedPath(rect, 16);

            var logo = GetLogo();
            if (logo == null)
            {
                using var fallback = new SolidBrush(AppTheme.Accent);
                e.Graphics.FillPath(fallback, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    "VG",
                    AppTheme.Font(9f, FontStyle.Bold),
                    ClientRectangle,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            using var imagePath = AppTheme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 14);
            var previousClip = e.Graphics.Clip;
            e.Graphics.SetClip(imagePath);
            AppTheme.DrawImageCover(e.Graphics, logo, new Rectangle(0, 0, Width, Height));
            e.Graphics.Clip = previousClip;
        }

        private static Image? GetLogo()
        {
            if (_logo != null)
                return _logo;

            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "valcongrup-logo.jpg");
            if (!File.Exists(path))
                return null;

            _logo = Image.FromFile(path);
            return _logo;
        }
    }

    private sealed class InitialAvatar : Control
    {
        private readonly string _initials;

        public InitialAvatar(string initials)
        {
            _initials = initials;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using var path = new GraphicsPath();
                path.AddEllipse(0, 0, Width, Height);
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 4 || Height < 4)
                return;

            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var grad = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(255, 164, 92),
                AppTheme.Accent,
                35f);
            e.Graphics.FillEllipse(grad, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(
                e.Graphics,
                _initials.ToUpperInvariant(),
                AppTheme.Font(11f, FontStyle.Bold),
                ClientRectangle,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
