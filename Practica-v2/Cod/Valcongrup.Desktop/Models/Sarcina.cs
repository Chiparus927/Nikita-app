namespace Valcongrup.Models;

public class Sarcina
{
    public int Id { get; set; }
    public int IdProiect { get; set; }
    public string NumeProiect { get; set; } = string.Empty;
    public int? IdResponsabil { get; set; }
    public string NumeResponsabil { get; set; } = string.Empty;
    public string Titlu { get; set; } = string.Empty;
    public string Descriere { get; set; } = string.Empty;
    public string Prioritate { get; set; } = "Medie";
    public string Status { get; set; } = "Noua";
    public DateTime? DataStart { get; set; }
    public DateTime? DataTermen { get; set; }
}
