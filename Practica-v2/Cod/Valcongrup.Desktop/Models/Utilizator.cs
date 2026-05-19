namespace Valcongrup.Models;

public class Utilizator
{
    public int Id { get; set; }
    public string Nume { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public string NumeRol { get; set; } = string.Empty;
    public string Rol { get => NumeRol; set => NumeRol = value; }
    public bool Activ { get; set; } = true;
    public bool IsApproved { get; set; } = true;
    public string NumeCompanie { get; set; } = string.Empty;
}

