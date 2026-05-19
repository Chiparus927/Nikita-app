using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Clienti")]
public class ClientImobil
{
    [Key]
    [Column("id_client")]
    public int IdClient { get; set; }

    [Column("nume")]
    [MaxLength(50)]
    public string? Nume { get; set; }

    [Column("prenume")]
    [MaxLength(50)]
    public string? Prenume { get; set; }

    [Column("telefon")]
    [MaxLength(20)]
    public string? Telefon { get; set; }

    [Column("email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    public ICollection<ContractImobil> Contracte { get; set; } = new List<ContractImobil>();
}
