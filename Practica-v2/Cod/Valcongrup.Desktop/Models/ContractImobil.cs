using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Contracte")]
public class ContractImobil
{
    [Key]
    [Column("id_contract")]
    public int IdContract { get; set; }

    [Column("data_finalizare")]
    public DateOnly? DataFinalizare { get; set; }

    [Column("id_client")]
    public int? IdClient { get; set; }

    [ForeignKey(nameof(IdClient))]
    public ClientImobil? Client { get; set; }

    [Column("id_unitate")]
    public int? IdUnitate { get; set; }

    [ForeignKey(nameof(IdUnitate))]
    public UnitateImobiliara? Unitate { get; set; }
}
