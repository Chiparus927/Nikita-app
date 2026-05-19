using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Echipe_Constructii")]
public class EchipaConstructie
{
    [Key]
    [Column("id_echipa")]
    public int IdEchipa { get; set; }

    [Column("nume_echipa")]
    [MaxLength(100)]
    public string NumeEchipa { get; set; } = string.Empty;

    [Column("numar_membri")]
    public int? NumarMembri { get; set; }

    public ICollection<ProiectImobil> Proiecte { get; set; } = new List<ProiectImobil>();
}
