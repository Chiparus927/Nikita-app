using System.Drawing.Drawing2D;
using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;
using Valcongrup.Models;

namespace Valcongrup.UserControls;

public class UcDashboard : UserControl
{
    private const int PagePadding = 48;
    private const int SectionGap = 18;
    private static Color BgMain => AppTheme.Shell;
    private static Color CardWhite => AppTheme.Card;
    private static Color TextDark => AppTheme.TextOnLight;
    private static Color TextGray => AppTheme.TextOnLightSecondary;
    private static Color TextMuted => AppTheme.IsDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(148, 163, 184);
    private static Color BlueDark => AppTheme.Accent;
    private static Color BlueAccent => Color.FromArgb(255, 185, 122);
    private static Color Border => AppTheme.Border;
    private static Color Red => AppTheme.Danger;

    private Label? _lblProiecte;
    private Label? _lblSarcini;
    private Label? _lblEchipa;
    private Label? _lblBuget;
    private DataGridView? _requestsGrid;
    private Label? _emptyRequestsLabel;
    private Panel? _chartPanel;
    private readonly List<Proiect> _pendingRequests = new();
    private readonly List<WeeklyProjectProgress> _weeklyProgress = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 60_000 };
    private bool _isLoadingDashboardData;

    public UcDashboard()
    {
        try
        {
            DoubleBuffered = true;
            BackColor = BgMain;
            Padding = Padding.Empty;
            BuildDashboard();
            Load += async (_, _) =>
            {
                await LoadDashboardData();
                _refreshTimer.Start();
            };
            Disposed += (_, _) => _refreshTimer.Stop();
            _refreshTimer.Tick += async (_, _) => await LoadDashboardData();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            BuildFallback(ex);
        }
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

    private void BuildDashboard()
    {
        SuspendLayout();
        Controls.Clear();
        Controls.Add(BuildTablePanel());
        Controls.Add(BuildMiddlePanel());
        Controls.Add(BuildCardPanel());
        Controls.Add(BuildHeaderPanel());
        ResumeLayout(true);
    }

    private Panel BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 132,
            BackColor = BgMain,
            Padding = new Padding(PagePadding, 18, PagePadding, 0)
        };

        var title = new Label
        {
            Text = "Executive Dashboard",
            Font = AppTheme.Font(24f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            BackColor = BgMain
        };
        var subtitle = new Label
        {
            Text = "Privire rapida asupra proiectelor, bugetelor si solicitarilor active.",
            Font = AppTheme.Font(10.5f),
            ForeColor = TextGray,
            AutoSize = true,
            BackColor = BgMain
        };

        var btnExport = ModernUi.GhostButton("Export", 116);
        var btnNew = AppTheme.AccentButton("+ Proiect", 146, 42);
        btnNew.Visible = Session.IsManagerOrAdmin();
        btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExport.Click += (_, _) => ExportToPdf();
        btnNew.Click += (_, _) => CreateNewProject();

        void PlaceActions()
        {
            if (panel.Width <= PagePadding * 2)
                return;

            var right = panel.Width - PagePadding;
            title.Location = new Point(PagePadding, 10);
            subtitle.Location = new Point(PagePadding, title.Bottom + 10);

            if (btnNew.Visible)
            {
                btnNew.Location = new Point(right - btnNew.Width, 32);
                right -= btnNew.Width + 12;
            }
            btnExport.Location = new Point(right - btnExport.Width, 30);
        }

        panel.Controls.AddRange(new Control[] { title, subtitle, btnExport, btnNew });
        panel.Resize += (_, _) => PlaceActions();
        panel.HandleCreated += (_, _) => PlaceActions();
        panel.VisibleChanged += (_, _) => PlaceActions();
        PlaceActions();
        return panel;
    }

    private Panel BuildCardPanel()
    {
        var strip = new Panel
        {
            Dock = DockStyle.Top,
            Height = 178,
            BackColor = BgMain
        };

        var cards = new[]
        {
            CreateStatCard("Proiecte active", "Proiecte in derulare", Color.FromArgb(255, 245, 235), BlueDark, out _lblProiecte),
            CreateStatCard("Sarcini", "Necesita atentie", Color.FromArgb(255, 247, 237), AppTheme.Accent, out _lblSarcini),
            CreateStatCard("Membri", "Echipe implicate", Color.FromArgb(255, 245, 235), Color.FromArgb(255, 164, 92), out _lblEchipa),
            CreateStatCard("Buget alocat", "Total operational", Color.FromArgb(255, 245, 235), Color.FromArgb(180, 101, 32), out _lblBuget)
        };
        strip.Controls.AddRange(cards);

        void LayoutCards()
        {
            var available = strip.ClientSize.Width - PagePadding * 2 - SectionGap * (cards.Length - 1);
            var width = Math.Max(220, available / cards.Length);
            var x = PagePadding;
            foreach (var card in cards)
            {
                card.SetBounds(x, 10, width, 148);
                x += width + SectionGap;
            }
        }

        strip.Resize += (_, _) => LayoutCards();
        strip.HandleCreated += (_, _) => LayoutCards();

        return strip;
    }

    private RoundedPanel CreateStatCard(string title, string subtitle, Color accentBg, Color accent, out Label valueLabel)
    {
        var card = new RoundedPanel
        {
            Width = 250,
            Height = 148,
            Radius = 18,
            BackColor = CardWhite,
            BorderColor = Border,
            Margin = Padding.Empty,
            Padding = new Padding(18, 16, 18, 18)
        };

        var accentPill = new Panel
        {
            Size = new Size(44, 12),
            BackColor = accentBg,
            Location = new Point(18, 18)
        };
        AppTheme.ApplyRoundedRegion(accentPill, 6);
        accentPill.Resize += (_, _) => AppTheme.ApplyRoundedRegion(accentPill, 6);

        var titleLabel = new Label
        {
            Text = title.ToUpperInvariant(),
            Font = AppTheme.Font(8.5f, FontStyle.Bold),
            ForeColor = TextGray,
            AutoSize = true,
            Location = new Point(18, 38),
            BackColor = CardWhite
        };

        var value = new Label
        {
            Text = "--",
            Font = AppTheme.Font(23f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(card.Width - 36, 42),
            Location = new Point(18, 66),
            BackColor = CardWhite,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subLabel = new Label
        {
            Text = subtitle,
            Font = AppTheme.Font(9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(card.Width - 36, 22),
            Location = new Point(18, 112),
            BackColor = CardWhite,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var valueAccent = new Panel
        {
            Size = new Size(8, 8),
            BackColor = accent,
            Location = new Point(card.Width - 26, 22)
        };
        AppTheme.ApplyRoundedRegion(valueAccent, 4);
        valueAccent.Resize += (_, _) => AppTheme.ApplyRoundedRegion(valueAccent, 4);

        void PerformCardLayout()
        {
            valueAccent.Location = new Point(card.Width - 26, 22);
            value.Width = card.Width - 36;
            subLabel.Width = card.Width - 36;
        }

        card.Resize += (_, _) => PerformCardLayout();

        valueLabel = value;
        card.Controls.AddRange(new Control[] { accentPill, titleLabel, value, subLabel, valueAccent });
        
        PerformCardLayout();
        return card;
    }

    private Panel BuildMiddlePanel()
    {
        const int outerHeight = 344;
        const int padding = PagePadding;
        const int gap = SectionGap;

        var outer = new Panel { Dock = DockStyle.Top, Height = outerHeight, BackColor = BgMain };

        _chartPanel = new RoundedPanel
        {
            Radius = 18,
            BackColor = CardWhite,
            BorderColor = Border,
            Padding = new Padding(20, 18, 20, 18)
        };
        _chartPanel.Paint += ChartPanel_Paint;

        var projectCard = new RoundedPanel
        {
            Radius = 18,
            BackColor = CardWhite,
            BorderColor = Border
        };

        var hero = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(56, 38, 22) };
        try
        {
            var imgPath = Path.Combine(Application.StartupPath, "Assets", "stadion-cricova-fc-barcelona.png");
            if (File.Exists(imgPath))
                hero.Image = Image.FromFile(imgPath);
        }
        catch
        {
        }

        hero.Paint += (_, e) =>
        {
            using var overlay = new LinearGradientBrush(
                new Rectangle(0, 0, hero.Width, hero.Height),
                Color.FromArgb(10, 255, 255, 255),
                Color.FromArgb(120, 56, 38, 22),
                90f);
            e.Graphics.FillRectangle(overlay, 0, 0, hero.Width, hero.Height);
            using var font = AppTheme.Font(8.5f, FontStyle.Bold);
            using var fill = new SolidBrush(Color.FromArgb(220, AppTheme.Accent));
            using var textBrush = new SolidBrush(Color.White);
            var text = "Featured project";
            var size = e.Graphics.MeasureString(text, font);
            var rect = new Rectangle(18, hero.Height - 42, (int)size.Width + 22, 26);
            using var path = AppTheme.RoundedPath(rect, 10);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawString(text, font, textBrush, rect.X + 11, rect.Y + 5);
        };

        var projectName = new Label
        {
            Text = "Stadion Cricova FC Barcelona",
            Font = AppTheme.Font(15f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            BackColor = CardWhite
        };
        var projectMeta = new Label
        {
            Text = "Estimare livrare · 2030",
            Font = AppTheme.Font(10f),
            ForeColor = TextGray,
            AutoSize = true,
            BackColor = CardWhite
        };
        var projectBody = new Label
        {
            Text = "Coordonare stadion, teren si infrastructura pentru un proiect sportiv premium.",
            Font = AppTheme.Font(9.5f),
            ForeColor = TextMuted,
            AutoSize = false,
            BackColor = CardWhite
        };

        projectCard.Controls.AddRange(new Control[] { hero, projectName, projectMeta, projectBody });

        void Layout()
        {
            var available = outer.Width - padding * 2;
            var leftWidth = (int)(available * 0.60f) - gap / 2;
            var rightWidth = available - leftWidth - gap;

            _chartPanel!.SetBounds(padding, 8, leftWidth, outerHeight - 24);
            projectCard.SetBounds(padding + leftWidth + gap, 8, rightWidth, outerHeight - 24);

            var heroHeight = (int)(projectCard.Height * 0.56f);
            hero.SetBounds(0, 0, projectCard.Width, heroHeight);
            projectName.Location = new Point(20, hero.Bottom + 18);
            projectMeta.Location = new Point(20, projectName.Bottom + 6);
            projectBody.Location = new Point(20, projectMeta.Bottom + 10);
            projectBody.Size = new Size(projectCard.Width - 40, 42);
        }

        outer.Resize += (_, _) => Layout();
        outer.Controls.Add(_chartPanel);
        outer.Controls.Add(projectCard);
        Load += (_, _) => Layout();
        return outer;
    }

    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (_chartPanel == null)
            return;

        var g = e.Graphics;
        AppTheme.ApplyHighQualityGraphics(g);

        using var titleBrush = new SolidBrush(TextDark);
        using var subBrush = new SolidBrush(TextGray);
        using var gridPen = new Pen(AppTheme.IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(245, 231, 220), 1);
        using var finishedBrush = new SolidBrush(BlueDark);
        using var activeBrush = new SolidBrush(BlueAccent);
        using var shadowBrush = new SolidBrush(Color.FromArgb(16, 120, 74, 32));
        using var titleFont = AppTheme.Font(13f, FontStyle.Bold);
        using var legendFont = AppTheme.Font(9f, FontStyle.Bold);
        using var dayFont = AppTheme.Font(8.5f, FontStyle.Bold);

        g.DrawString("Progres saptamanal proiecte", titleFont, titleBrush, 20, 18);

        DrawLegendPill(g, 20, 52, finishedBrush.Color, "Finalizat", legendFont);
        DrawLegendPill(g, 132, 52, activeBrush.Color, "In lucru", legendFont);

        const int top = 92;
        const int left = 24;
        const int right = 24;
        const int bottom = 40;
        var chartWidth = _chartPanel.Width - left - right;
        var chartHeight = _chartPanel.Height - top - bottom;
        if (chartWidth <= 120 || chartHeight <= 80)
            return;

        string[] days = { "L", "Ma", "Mi", "J", "V", "S", "D" };
        var data = _weeklyProgress.Count == 7
            ? _weeklyProgress.OrderBy(x => x.DayIndex).ToList()
            : Enumerable.Range(0, 7).Select(i => new WeeklyProjectProgress(i, 0, 0)).ToList();
        var maxTotal = Math.Max(1, data.Max(x => x.Finalizate + x.InLucru));

        if (data.All(x => x.Finalizate == 0 && x.InLucru == 0))
        {
            TextRenderer.DrawText(
                g,
                "Nu exista proiecte create sau actualizate saptamana aceasta.",
                AppTheme.Font(9.5f),
                new Rectangle(left, top + chartHeight / 2 - 18, chartWidth, 36),
                TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        for (var i = 0; i < 4; i++)
        {
            var y = top + i * chartHeight / 4;
            g.DrawLine(gridPen, left, y, left + chartWidth, y);
        }

        var step = chartWidth / 7f;
        var barWidth = Math.Min(34, Math.Max(18, (int)(step * 0.54f)));
        for (var i = 0; i < 7; i++)
        {
            var finalizate = data[i].Finalizate;
            var inLucru = data[i].InLucru;
            var x = (int)(left + i * step + (step - barWidth) / 2f);
            var baseY = top + chartHeight;
            var finishedHeight = (int)Math.Round(finalizate / (double)maxTotal * chartHeight);
            var activeHeight = (int)Math.Round(inLucru / (double)maxTotal * chartHeight);

            if (finishedHeight + activeHeight > 0)
            {
                g.FillRectangle(shadowBrush, x + 1, baseY - finishedHeight - activeHeight + 2, barWidth, activeHeight);
                if (finishedHeight > 0)
                    g.FillRectangle(finishedBrush, x, baseY - finishedHeight, barWidth, finishedHeight);
                if (activeHeight > 0)
                    g.FillRectangle(activeBrush, x, baseY - finishedHeight - activeHeight, barWidth, activeHeight);
            }

            var size = g.MeasureString(days[i], dayFont);
            g.DrawString(days[i], dayFont, subBrush, x + barWidth / 2f - size.Width / 2f, baseY + 8);
        }
    }

    private static void DrawLegendPill(Graphics g, int x, int y, Color color, string text, Font font)
    {
        using var fill = new SolidBrush(AppTheme.IsDarkMode ? Color.FromArgb(50, color) : Color.FromArgb(242, color));
        using var dot = new SolidBrush(color);
        using var textBrush = new SolidBrush(AppTheme.TextOnLightSecondary);

        var textSize = g.MeasureString(text, font);
        var rect = new Rectangle(x, y, (int)textSize.Width + 34, 24);
        using var path = AppTheme.RoundedPath(rect, 12);
        g.FillPath(fill, path);
        g.FillEllipse(dot, x + 8, y + 8, 8, 8);
        g.DrawString(text, font, textBrush, x + 20, y + 5);
    }

    private Panel BuildTablePanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = BgMain, Padding = new Padding(PagePadding, 0, PagePadding, 32) };
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            BackColor = CardWhite,
            BorderColor = Border,
            Padding = new Padding(20, 0, 20, 20)
        };

        var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = CardWhite };
        var title = new Label
        {
            Text = "Cereri proiecte clienti si subcontractori",
            Font = AppTheme.Font(13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Location = new Point(4, 22),
            BackColor = CardWhite
        };
        var filterLabel = new Label
        {
            Text = "Filtru:",
            Font = AppTheme.Font(8.75f, FontStyle.Bold),
            ForeColor = TextGray,
            AutoSize = true,
            BackColor = CardWhite
        };
        var cmb = AppTheme.Combo();
        cmb.Width = 140;
        cmb.Items.AddRange(new object[] { "Toate cererile", "Cerere", "Activ", "Refuzat" });
        cmb.SelectedIndex = 0;
        cmb.SelectedIndexChanged += (_, _) => RefreshRequestsGrid(cmb.SelectedItem?.ToString());

        header.Resize += (_, _) =>
        {
            cmb.Location = new Point(header.Width - cmb.Width - 4, 20);
            filterLabel.Location = new Point(cmb.Left - filterLabel.Width - 12, 24);
        };
        header.Controls.AddRange(new Control[] { title, filterLabel, cmb });

        _requestsGrid = ModernUi.Grid();
        _requestsGrid.Dock = DockStyle.Fill;
        _requestsGrid.BackgroundColor = CardWhite;
        _requestsGrid.BorderStyle = BorderStyle.None;
        _requestsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _requestsGrid.RowTemplate.Height = 48;
        _requestsGrid.ColumnHeadersHeight = 46;
        _requestsGrid.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        _requestsGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        _requestsGrid.Columns.Clear();
        _requestsGrid.AutoGenerateColumns = false;
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "ID", DataPropertyName = "Id", Visible = false });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "client", HeaderText = "CLIENT", DataPropertyName = "NumeClient", FillWeight = 24, MinimumWidth = 170 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "tip", HeaderText = "PROIECT", DataPropertyName = "Descriere", FillWeight = 28, MinimumWidth = 200 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "valoare", HeaderText = "BUGET", DataPropertyName = "BugetFmt", FillWeight = 18, MinimumWidth = 120 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "status", HeaderText = "STATUS", DataPropertyName = "Status", FillWeight = 14, MinimumWidth = 110 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "actiuni", HeaderText = "ACTIUNI", FillWeight = 16, MinimumWidth = 170 });
        _requestsGrid.CellPainting += Grid_CellPainting;
        _requestsGrid.CellClick += Grid_CellClick;

        _emptyRequestsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Nu exista cereri in asteptare.",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextGray,
            Font = AppTheme.Font(10.5f),
            BackColor = CardWhite,
            Visible = false
        };

        card.Controls.Add(_emptyRequestsLabel);
        card.Controls.Add(_requestsGrid);
        card.Controls.Add(header);
        outer.Controls.Add(card);
        return outer;
    }

    private void RefreshRequestsGrid(string? filter)
    {
        if (_requestsGrid == null)
            return;

        var source = filter == null || filter == "Toate cererile"
            ? _pendingRequests
            : _pendingRequests.Where(p => p.Status == filter).ToList();

        var table = new System.Data.DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("NumeClient");
        table.Columns.Add("Descriere");
        table.Columns.Add("BugetFmt");
        table.Columns.Add("Status");

        foreach (var proiect in source)
            table.Rows.Add(
                proiect.Id,
                proiect.NumeClient ?? "N/A",
                proiect.Descriere ?? "-",
                $"{proiect.BugetTotal:N0} RON",
                proiect.Status);

        _requestsGrid.DataSource = table;
        _requestsGrid.Visible = source.Count > 0;
        if (_emptyRequestsLabel != null)
            _emptyRequestsLabel.Visible = source.Count == 0;
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != 5 || e.CellBounds.Width < 10)
            return;

        e.Handled = true;
        e.Graphics!.FillRectangle(new SolidBrush(CardWhite), e.CellBounds);
        using var rowPen = new Pen(Border, 1);
        e.Graphics.DrawLine(rowPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

        DrawActionButton(e.Graphics, new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y + 9, 72, 30), BlueDark, "Accepta", true);
        DrawActionButton(e.Graphics, new Rectangle(e.CellBounds.X + 94, e.CellBounds.Y + 9, 70, 30), Color.White, "Refuza", false);
    }

    private void DrawActionButton(Graphics g, Rectangle rect, Color fill, string text, bool primary)
    {
        AppTheme.ApplyHighQualityGraphics(g);
        using var path = AppTheme.RoundedPath(rect, 10);
        using var fillBrush = new SolidBrush(fill);
        using var borderPen = new Pen(primary ? fill : Border, 1);
        using var textBrush = new SolidBrush(primary ? Color.White : TextGray);
        using var font = AppTheme.Font(8f, FontStyle.Bold);
        g.FillPath(fillBrush, path);
        g.DrawPath(borderPen, path);
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush, rect.X + rect.Width / 2f - size.Width / 2f, rect.Y + 6);
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex != 5)
            return;

        var cursor = grid.PointToClient(Cursor.Position);
        var bounds = grid.GetCellDisplayRectangle(5, e.RowIndex, false);
        var acceptRect = new Rectangle(bounds.X + 12, bounds.Y + 9, 72, 30);
        var rejectRect = new Rectangle(bounds.X + 94, bounds.Y + 9, 70, 30);

        var idCell = grid.Rows[e.RowIndex].Cells["id"].Value;
        if (!int.TryParse(idCell?.ToString(), out var projectId) && idCell is not int intId)
            return;
        if (idCell is int directId)
            projectId = directId;

        if (acceptRect.Contains(cursor))
        {
            try
            {
                var repo = new ProiecteRepository();
                repo.AcceptRequest(projectId, Session.CurrentUser!.Id);
                new JurnalRepository().Log(Session.CurrentUser.Id, projectId, $"Cerere acceptata (id={projectId})", "Proiect", projectId);
                _ = LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la acceptare: " + ex.Message, "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else if (rejectRect.Contains(cursor))
        {
            if (MessageBox.Show("Refuzi aceasta cerere?", "Confirmare", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var repo = new ProiecteRepository();
                repo.RejectRequest(projectId);
                new JurnalRepository().Log(Session.CurrentUser!.Id, projectId, $"Cerere refuzata (id={projectId})", "Proiect", projectId);
                _ = LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la refuzare: " + ex.Message, "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task LoadDashboardData()
    {
        if (_isLoadingDashboardData)
            return;

        _isLoadingDashboardData = true;
        try
        {
            var snapshot = await Task.Run(() =>
            {
                var dashRepo = new DashboardRepository();
                var proiecteRepo = new ProiecteRepository();
                return new
                {
                    Kpis = dashRepo.GetKpis(),
                    WeeklyProgress = dashRepo.GetWeeklyProjectProgress(DateTime.Today),
                    Requests = IsManager() ? proiecteRepo.GetPendingRequests() : new List<Proiect>()
                };
            });

            SafeUi(() =>
            {
                if (_lblProiecte != null) _lblProiecte.Text = snapshot.Kpis.ActiveProjectsCount.ToString();
                if (_lblSarcini != null) _lblSarcini.Text = snapshot.Kpis.PendingTasksCount.ToString();
                if (_lblEchipa != null) _lblEchipa.Text = snapshot.Kpis.TeamMembersCount.ToString();
                if (_lblBuget != null) _lblBuget.Text = FormatBudget(snapshot.Kpis.TotalUtilizedBudget);

                _pendingRequests.Clear();
                _pendingRequests.AddRange(snapshot.Requests);
                _weeklyProgress.Clear();
                _weeklyProgress.AddRange(snapshot.WeeklyProgress);
                RefreshRequestsGrid("Toate cererile");
                _chartPanel?.Invalidate();
            });
        }
        catch (Exception ex)
        {
            LogCrash(ex);
        }
        finally
        {
            _isLoadingDashboardData = false;
        }
    }

    private string FormatBudget(decimal value) =>
        value >= 1_000_000 ? $"{value / 1_000_000:0.##}M" :
        value >= 1_000 ? $"{value / 1_000:0.#}k" :
        $"{value:N0}";

    private void ExportToPdf() =>
        MessageBox.Show("Functia de export PDF va fi implementata.", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void CreateNewProject()
    {
        if (!Session.IsManagerOrAdmin())
        {
            MessageBox.Show("Doar rolurile Manager si Admin pot adauga proiecte.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new DialogProiect();
        if (dialog.ShowDialog(ParentForm) != DialogResult.OK)
            return;

        var projectRepo = new ProiecteRepository();
        var projectId = projectRepo.Insert(dialog.Proiect);
        new JurnalRepository().Log(Session.CurrentUser!.Id, projectId, $"A adaugat proiectul '{dialog.Proiect.Nume}'", "Proiect", projectId);
        _ = LoadDashboardData();
    }

    private void BuildFallback(Exception ex)
    {
        Controls.Clear();
        BackColor = CardWhite;
        Padding = new Padding(40);
        Controls.AddRange(new Control[]
        {
            new Label
            {
                Text = "Executive Dashboard",
                Font = AppTheme.Font(20f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(40, 28)
            },
            new Label
            {
                Text = $"Eroare la incarcare: {ex.Message}",
                Font = AppTheme.Font(10f),
                ForeColor = Red,
                AutoSize = false,
                Size = new Size(700, 60),
                Location = new Point(40, 76),
                BackColor = CardWhite
            }
        });
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Valcongrup");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "dashboard-crash.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n{ex}\r\n\r\n");
        }
        catch
        {
        }
    }

    private static bool IsManager() =>
        Session.IsManagerOrAdmin();
}
