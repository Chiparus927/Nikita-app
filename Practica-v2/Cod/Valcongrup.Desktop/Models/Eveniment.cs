namespace Valcongrup.Models;

public class Eveniment
{
    public int Id { get; set; }
    public int? IdProiect { get; set; }
    public string Titlu { get; set; } = string.Empty;
    public string Descriere { get; set; } = string.Empty;
    public string Tip { get; set; } = "Altul";
    public DateTime DataStart { get; set; } = DateTime.Now;
    public DateTime DataSfarsit { get; set; } = DateTime.Now.AddHours(1);
    public string Culoare { get; set; } = "#F97316";
}
