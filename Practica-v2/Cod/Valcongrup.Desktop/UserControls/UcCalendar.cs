using System.Data;
using Valcongrup.Data;
using Valcongrup.Forms.Dialogs;

namespace Valcongrup.UserControls;

public class UcCalendar : UserControl
{
    private readonly MonthCanvas _calendar = new();
    private readonly DataGridView _grid = ModernUi.Grid();
    private readonly EvenimenteRepository _repo = new();
    private DateTime _selectedDate = DateTime.Today;

    public UcCalendar()
    {
        BackColor = AppTheme.Shell;
        Padding = new Padding(48, 0, 32, 32);

        var add = AppTheme.AccentButton("Adauga Eveniment", 220, 50);
        add.Visible = Session.IsManagerOrAdmin();
        add.Click += (_, _) => Add();

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = AppTheme.Shell,
            Padding = new Padding(0, 22, 0, 0)
        };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 172));

        var calendarCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 16,
            BackColor = AppTheme.Card,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 16)
        };
        _calendar.Dock = DockStyle.Fill;
        _calendar.DateSelected += date =>
        {
            _selectedDate = date;
            LoadData(date);
        };
        calendarCard.Controls.Add(_calendar);

        var eventsCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 12,
            BackColor = AppTheme.Card,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 12)
        };
        eventsCard.Controls.Add(_grid);

        body.Controls.Add(calendarCard, 0, 0);
        body.Controls.Add(eventsCard, 0, 1);
        Controls.Add(body);
        Controls.Add(ModernUi.PageHeader("Calendar", "Planifica evenimentele proiectelor pe intreaga pagina de lucru.", add));

        LoadData(_selectedDate);
    }

    private void LoadData(DateTime date)
    {
        UiFactory.Try(() =>
        {
            var table = _repo.GetByDate(date);
            _grid.DataSource = table;
            _calendar.SetEvents(_repo.GetByMonth(date));
        });
    }

    private void Add()
    {
        if (!Session.IsManagerOrAdmin())
        {
            MessageBox.Show("Doar rolurile Manager si Admin pot adauga evenimente.", "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new DialogEveniment(_selectedDate);
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        UiFactory.Try(() =>
        {
            var id = _repo.Insert(dlg.Eveniment);
            new JurnalRepository().Log(Session.CurrentUser!.Id, dlg.Eveniment.IdProiect, $"A adaugat evenimentul '{dlg.Eveniment.Titlu}'", "Eveniment", id);
            LoadData(_selectedDate);
        });
    }

    private sealed class MonthCanvas : Control
    {
        private readonly HashSet<int> _eventDays = new();
        private DateTime _visibleMonth = DateTime.Today;
        public event Action<DateTime>? DateSelected;
        public DateTime SelectedDate { get; private set; } = DateTime.Today;

        public MonthCanvas()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Font = AppTheme.Font(10f);
        }

        public void SetEvents(DataTable table)
        {
            _eventDays.Clear();
            foreach (DataRow row in table.Rows)
                if (row["Start"] is DateTime dt && dt.Month == _visibleMonth.Month && dt.Year == _visibleMonth.Year)
                    _eventDays.Add(dt.Day);
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var hit = HitTestDay(e.Location);
            if (!hit.HasValue)
                return;

            SelectedDate = hit.Value;
            _visibleMonth = SelectedDate;
            DateSelected?.Invoke(SelectedDate.Date);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ApplyHighQualityGraphics(e.Graphics);
            e.Graphics.Clear(AppTheme.Card);

            var title = _visibleMonth.ToString("MMMM yyyy");
            TextRenderer.DrawText(e.Graphics, title, AppTheme.Font(18f, FontStyle.Bold), new Rectangle(8, 8, Width - 16, 44), AppTheme.TextOnLight, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var days = new[] { "Lun", "Mar", "Mie", "Joi", "Vin", "Sam", "Dum" };
            var gridTop = 66;
            var cellW = Math.Max(1, Width / 7);
            var cellH = Math.Max(1, (Height - gridTop) / 7);

            for (var i = 0; i < 7; i++)
                TextRenderer.DrawText(e.Graphics, days[i], AppTheme.Font(9.5f, FontStyle.Bold), new Rectangle(i * cellW, gridTop, cellW, cellH), AppTheme.TextOnLightSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            using var line = new Pen(AppTheme.Border);
            for (var col = 0; col <= 7; col++)
                e.Graphics.DrawLine(line, col * cellW, gridTop + cellH, col * cellW, Height - 1);
            for (var row = 1; row <= 7; row++)
                e.Graphics.DrawLine(line, 0, gridTop + row * cellH, Width - 1, gridTop + row * cellH);

            var first = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1);
            var startOffset = ((int)first.DayOfWeek + 6) % 7;
            var daysInMonth = DateTime.DaysInMonth(first.Year, first.Month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var index = startOffset + day - 1;
                var row = index / 7 + 1;
                var col = index % 7;
                var rect = new Rectangle(col * cellW + 1, gridTop + row * cellH + 1, cellW - 2, cellH - 2);
                if (SelectedDate.Date == new DateTime(first.Year, first.Month, day))
                {
                    using var fill = new SolidBrush(AppTheme.IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(255, 247, 237));
                    e.Graphics.FillRectangle(fill, rect);
                    using var edge = new Pen(AppTheme.Accent, 2f);
                    e.Graphics.DrawRectangle(edge, rect);
                }

                TextRenderer.DrawText(e.Graphics, day.ToString(), AppTheme.Font(11f, FontStyle.Bold), new Rectangle(rect.X + 10, rect.Y + 8, 44, 28), AppTheme.TextOnLight, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                if (_eventDays.Contains(day))
                {
                    using var dot = new SolidBrush(AppTheme.Accent);
                    e.Graphics.FillEllipse(dot, rect.X + 14, rect.Bottom - 22, 9, 9);
                }
            }
        }

        private DateTime? HitTestDay(Point point)
        {
            var gridTop = 66;
            if (point.Y < gridTop)
                return null;

            var cellW = Math.Max(1, Width / 7);
            var cellH = Math.Max(1, (Height - gridTop) / 7);
            var col = point.X / cellW;
            var row = (point.Y - gridTop) / cellH - 1;
            if (col < 0 || col > 6 || row < 0 || row > 5)
                return null;

            var first = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1);
            var startOffset = ((int)first.DayOfWeek + 6) % 7;
            var day = row * 7 + col - startOffset + 1;
            if (day < 1 || day > DateTime.DaysInMonth(first.Year, first.Month))
                return null;

            return new DateTime(first.Year, first.Month, day);
        }
    }
}
