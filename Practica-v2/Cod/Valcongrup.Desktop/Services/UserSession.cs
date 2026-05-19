using Valcongrup.Desktop.Models;

namespace Valcongrup.Desktop.Services;

public class UserSession : IUserSession
{
    public SesiuneUtilizator? CurrentUser { get; set; }
}
