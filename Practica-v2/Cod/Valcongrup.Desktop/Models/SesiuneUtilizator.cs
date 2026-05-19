namespace Valcongrup.Desktop.Models;

/// <summary>Utilizator autentificat (tabel Utilizatori_App).</summary>
public sealed class SesiuneUtilizator
{
    public int Id { get; init; }
    public string Nume { get; init; } = string.Empty;
    public string Prenume { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
