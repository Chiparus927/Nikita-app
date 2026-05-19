using Valcongrup.Data;
using Valcongrup.Models;

namespace Valcongrup.UserControls;

public class UcBuget : UserControl
{
    private readonly ComboBox _proiect    = AppTheme.Combo();
    private readonly DataGridView _grid   = ModernUi.Grid();
    private readonly DataGridView _dist   = ModernUi.Grid();
    private readonly Label _lblTotal      = new() { AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55) };
    private readonly Label _lblUtilizat   = new() { AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55) };
    private readonly Label _lblRamas      = new() { AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55) };

    public UcBuget()
    {
        BackColor = AppTheme.Shell;
        Padding   = new Padding(48, 0, 32, 32);

        // Header action controls
        _proiect.Width = 260;
        _proiect.SelectedIndexChanged += (_, _) => LoadData();

        var btnAdd = AppTheme.AccentButton("+ Adaugă Cheltuială", 180, 50);
        btnAdd.Click += (_, _) => Add();

        Controls.Add(BuildContent());
        Controls.Add(ModernUi.PageHeader("Buget", "Monitorizează consumul bugetar pe proiecte.", _proiect, btnAdd));

        UiFactory.Try(() =>
        {
            _proiect.DataSource    = UiFactory.ProjectsLookup();
            _proiect.DisplayMember = "nume";
            _proiect.ValueMember   = "id";
            LoadData();
        });
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private Control BuildContent()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Shell };

        // KPI cards strip
        var cards = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = AppTheme.Shell };
        cards.Controls.Add(BuildKpiCard("Buget Total",    _lblTotal,    Color.FromArgb(59, 130, 246)));
        cards.Controls.Add(BuildKpiCard("Buget Utilizat", _lblUtilizat, AppTheme.Warning));
        cards.Controls.Add(BuildKpiCard("Buget Rămas",    _lblRamas,    AppTheme.Success));
        cards.Resize += (_, _) => LayoutKpiCards(cards);
        outer.Controls.Add(BuildGridsSection());
        outer.Controls.Add(cards);
        return outer;
    }

    private static void LayoutKpiCards(Panel strip)
    {
        const int gap = 16, pad = 0;
        int n  = strip.Controls.Count;
        int w  = (strip.Width - pad * 2 - gap * (n - 1)) / n;
        int h  = strip.Height - 20;
        for (int i = 0; i < n; i++)
            strip.Controls[i].SetBounds(pad + i * (w + gap), 10, w, h);
    }

    private static Panel BuildKpiCard(string title, Label valLbl, Color accent)
    {
        var card = new RoundedPanel { BackColor = AppTheme.Card, Radius = 16, Padding = new Padding(18) };
        card.Paint += (_, e) =>
        {
            using var pen = new System.Drawing.Pen(AppTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            e.Graphics.FillRectangle(new SolidBrush(accent), 0, 14, 4, card.Height - 28);
        };
        var lblTitle = new Label
        {
            Text = title, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(107, 114, 128), AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(18, 12)
        };
        valLbl.Text     = "0 MDL";
        valLbl.Location = new Point(18, 38);
        valLbl.BackColor = Color.Transparent;
        card.Controls.AddRange(new Control[] { lblTitle, valLbl });
        return card;
    }

    private Control BuildGridsSection()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill, BackColor = AppTheme.Shell,
            SplitterWidth = 12
        };

        var cardLeft = new RoundedPanel { Dock = DockStyle.Fill, Radius = 16, BackColor = AppTheme.Card, Padding = new Padding(0) };
        _grid.Dock = DockStyle.Fill;
        cardLeft.Controls.Add(_grid);

        var cardRight = new RoundedPanel { Dock = DockStyle.Fill, Radius = 16, BackColor = AppTheme.Card, Padding = new Padding(0) };
        _dist.Dock = DockStyle.Fill;
        cardRight.Controls.Add(_dist);

        split.Panel1.Controls.Add(cardLeft);
        split.Panel2.Controls.Add(cardRight);
        split.SplitterDistance = 720;
        return split;
    }

    // ── Data ──────────────────────────────────────────────────────────────────
    private void LoadData()
    {
        if (_proiect.SelectedValue == null) return;
        UiFactory.Try(() =>
        {
            var id       = Convert.ToInt32(_proiect.SelectedValue);
            var projects = new ProiecteRepository().GetAll();
            var p        = projects.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                var ramas   = p.BugetTotal - p.BugetUtilizat;
                var procent = p.BugetTotal == 0 ? 0m : ramas / p.BugetTotal * 100;

                void Update()
                {
                    _lblTotal.Text    = $"{p.BugetTotal:N0} MDL";
                    _lblUtilizat.Text = $"{p.BugetUtilizat:N0} MDL";
                    _lblRamas.Text    = $"{ramas:N0} MDL";
                    _lblRamas.ForeColor = procent > 20 ? AppTheme.Success : procent > 10 ? AppTheme.Warning : AppTheme.Danger;
                }
                if (InvokeRequired) Invoke(Update); else Update();
            }

            var tranzactii  = new BugetRepository().GetByProiect(id);
            var distributie = new BugetRepository().GetDistributiePeCategorie(id);

            void BindGrids()
            {
                _grid.DataSource = tranzactii;
                _dist.DataSource = distributie;
            }
            if (InvokeRequired) Invoke(BindGrids); else BindGrids();
        });
    }

    private void Add()
    {
        if (_proiect.SelectedValue == null)
        {
            MessageBox.Show("Selectați un proiect înainte de a adăuga o cheltuială.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var suma = Microsoft.VisualBasic.Interaction.InputBox("Suma cheltuială (MDL):", "Adaugă Cheltuială", "0");
        if (!decimal.TryParse(suma, out var val) || val <= 0) return;
        UiFactory.Try(() =>
        {
            var t = new TranzactieBuget
            {
                IdProiect = Convert.ToInt32(_proiect.SelectedValue),
                Suma      = val,
                Descriere = "Cheltuială adăugată manual"
            };
            var id = new BugetRepository().Insert(t);
            new JurnalRepository().Log(Session.CurrentUser!.Id, t.IdProiect, $"A adăugat cheltuiala {t.Suma:N2} MDL", "Buget", id);
            LoadData();
        });
    }
}
