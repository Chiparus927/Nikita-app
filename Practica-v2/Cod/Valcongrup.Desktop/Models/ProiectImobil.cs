using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Proiecte")]
public class ProiectImobil
{
    [Key]
    [Column("id_proiect")]
    public int IdProiect { get; set; }

    [Column("nume_proiect")]
    [MaxLength(100)]
    public string NumeProiect { get; set; } = string.Empty;

    [Column("adresa")]
    [MaxLength(255)]
    public string? Adresa { get; set; }

    [Column("data_incepere")]
    public DateOnly? DataIncepere { get; set; }

    [Column("id_echipa")]
    public int? IdEchipa { get; set; }

    [ForeignKey(nameof(IdEchipa))]
    public EchipaConstructie? Echipa { get; set; }

    public ICollection<UnitateImobiliara> Unitati { get; set; } = new List<UnitateImobiliara>();
}
