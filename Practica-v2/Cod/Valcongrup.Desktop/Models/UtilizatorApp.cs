using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Utilizatori_App")]
public class UtilizatorApp
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Column("parola_hash")]
    [MaxLength(255)]
    public string ParolaHash { get; set; } = string.Empty;

    [Column("nume")]
    [MaxLength(50)]
    public string Nume { get; set; } = string.Empty;

    [Column("prenume")]
    [MaxLength(50)]
    public string Prenume { get; set; } = string.Empty;

    [Column("activ")]
    public bool Activ { get; set; } = true;

    [Column("creat_la")]
    public DateTime CreatLa { get; set; }

    [Column("ultima_logare")]
    public DateTime? UltimaLogare { get; set; }
}
