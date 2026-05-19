using Valcongrup.Models;

namespace Valcongrup;

public static class Session
{
    public static Utilizator? CurrentUser { get; set; }

    public static string RoleName =>
        (CurrentUser?.NumeRol ?? CurrentUser?.Rol ?? string.Empty).Trim();

    public static bool IsAdmin()
    {
        return RoleName.Equals("Admin", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManager()
    {
        return RoleName.Equals("Manager", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManagerOrAdmin()
    {
        return IsAdmin() || IsManager();
    }

    public static bool IsClient()
    {
        return RoleName.Equals("Client", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSubcontractor()
    {
        return RoleName.Equals("Subcontractor", System.StringComparison.OrdinalIgnoreCase)
            || RoleName.Equals("Subcontractant", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanRequestProjects()
    {
        return IsClient() || IsSubcontractor();
    }
}

