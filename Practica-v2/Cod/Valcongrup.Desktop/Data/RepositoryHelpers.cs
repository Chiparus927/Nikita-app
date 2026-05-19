using System.Data;
using MySql.Data.MySqlClient;

namespace Valcongrup.Data;

public static class RepositoryHelpers
{
    public static DataTable Fill(string sql, Action<MySqlParameterCollection>? parameters = null)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(sql, conn);
        parameters?.Invoke(cmd.Parameters);
        using var adapter = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        adapter.Fill(dt);
        return dt;
    }
}
