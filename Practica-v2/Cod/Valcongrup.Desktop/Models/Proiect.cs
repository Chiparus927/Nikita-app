namespace Valcongrup.Models;

public class Proiect
{
    public int Id { get; set; }
    public string Nume { get; set; } = string.Empty;
    public string Descriere { get; set; } = string.Empty;
    public int? IdClient { get; set; }
    public string NumeClient { get; set; } = string.Empty;
    public int? IdManager { get; set; }
    public string NumeManager { get; set; } = string.Empty;
    public decimal BugetTotal { get; set; }
    public decimal BugetUtilizat { get; set; }
    public DateTime DataStart { get; set; } = DateTime.Today;
    public DateTime DataTermen { get; set; } = DateTime.Today.AddMonths(1);
    public int Progres { get; set; }
    public string Status { get; set; } = "Planificat";
}
