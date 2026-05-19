using Valcongrup.Desktop.Models;

namespace Valcongrup.Desktop.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string nume, string prenume, string email, string password,
        CancellationToken cancellationToken = default);

    Task<SesiuneUtilizator?> LoginAsync(string email, string password,
        CancellationToken cancellationToken = default);
}

public sealed class AuthResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public SesiuneUtilizator? User { get; init; }

    public static AuthResult Ok(SesiuneUtilizator user) => new() { Success = true, User = user };

    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
