using Valcongrup.Desktop.Models;

namespace Valcongrup.Desktop.Services;

public interface IUserSession
{
    SesiuneUtilizator? CurrentUser { get; set; }
}
