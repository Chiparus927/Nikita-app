using System.Data;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class EvenimenteRepository
{
    public DataTable GetByDate(DateTime data) => RepositoryHelpers.Fill(@"SELECT e.id, e.titlu AS Titlu, e.tip AS Tip, COALESCE(p.nume,'') AS Proiect, e.data_start AS Start, e.data_sfarsit AS Sfarsit FROM evenimente e LEFT JOIN proiecte p ON p.id=e.id_proiect WHERE DATE(e.data_start)=@data ORDER BY e.data_start", ps => ps.AddWithValue("@data", data.Date));

    public DataTable GetByMonth(DateTime data) => RepositoryHelpers.Fill(@"SELECT e.id, e.titlu AS Titlu, e.tip AS Tip, COALESCE(p.nume,'') AS Proiect, e.data_start AS Start, e.data_sfarsit AS Sfarsit FROM evenimente e LEFT JOIN proiecte p ON p.id=e.id_proiect WHERE YEAR(e.data_start)=@year AND MONTH(e.data_start)=@month ORDER BY e.data_start", ps =>
    {
        ps.AddWithValue("@year", data.Year);
        ps.AddWithValue("@month", data.Month);
    });

    public int Insert(Eveniment e)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO evenimente (id_proiect,titlu,descriere,tip,data_start,data_sfarsit,culoare)
VALUES (@p,@t,@d,@tip,@s,@f,@c); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@p", (object?)e.IdProiect ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@t", e.Titlu);
        cmd.Parameters.AddWithValue("@d", e.Descriere);
        cmd.Parameters.AddWithValue("@tip", e.Tip);
        cmd.Parameters.AddWithValue("@s", e.DataStart);
        cmd.Parameters.AddWithValue("@f", e.DataSfarsit);
        cmd.Parameters.AddWithValue("@c", e.Culoare);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
