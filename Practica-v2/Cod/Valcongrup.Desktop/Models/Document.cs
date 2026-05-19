namespace Valcongrup.Models;

public class Document
{
    public int Id { get; set; }
    public int IdProiect { get; set; }
    public int IdCategorie { get; set; }
    public int IdIncarcatDe { get; set; }
    public string NumeFisier { get; set; } = string.Empty;
    public string CaleFisier { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int DimensiuneKb { get; set; }
}
