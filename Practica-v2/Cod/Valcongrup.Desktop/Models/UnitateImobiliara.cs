using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valcongrup.Desktop.Models;

[Table("Unitati_Imobiliare")]
public class UnitateImobiliara
{
    [Key]
    [Column("id_unitate")]
    public int IdUnitate { get; set; }

    [Column("nr_apartament")]
    [MaxLength(10)]
    public string? NrApartament { get; set; }

    [Column("suprafata_mp")]
    public decimal? SuprafataMp { get; set; }

    [Column("pret_vanzare")]
    public decimal? PretVanzare { get; set; }

    /// <summary>In constructie, Disponibil, Vandut</summary>
    [Column("status_unitate")]
    [MaxLength(32)]
    public string StatusUnitate { get; set; } = "In constructie";

    [Column("id_proiect")]
    public int? IdProiect { get; set; }

    [ForeignKey(nameof(IdProiect))]
    public ProiectImobil? Proiect { get; set; }

    public ContractImobil? Contract { get; set; }
}
