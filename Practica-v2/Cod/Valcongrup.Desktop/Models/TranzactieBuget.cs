namespace Valcongrup.Models;

public class TranzactieBuget
{
    public int Id { get; set; }
    public int IdProiect { get; set; }
    public int? IdCategorie { get; set; }
    public string Categorie { get; set; } = string.Empty;
    public decimal Suma { get; set; }
    public string Descriere { get; set; } = string.Empty;
    public DateTime DataTranzactie { get; set; } = DateTime.Today;
}
