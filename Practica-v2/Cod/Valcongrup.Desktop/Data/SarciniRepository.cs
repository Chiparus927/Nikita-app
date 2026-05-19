using System.Data;
using MySql.Data.MySqlClient;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class SarciniRepository
{
    private const string SelectSql = @"SELECT s.id, s.id_proiect, COALESCE(p.nume,''), s.id_responsabil, COALESCE(CONCAT(u.prenume,' ',u.nume),''),
       s.titlu, COALESCE(s.descriere,''), s.prioritate, s.status, s.data_termen
FROM sarcini s
JOIN proiecte p ON p.id = s.id_proiect
LEFT JOIN utilizatori u ON u.id = s.id_responsabil";

    public List<Sarcina> GetAll() => Query(SelectSql + " ORDER BY s.data_termen IS NULL, s.data_termen");
    public DataTable LoadTasksData(string filterType, string? filterValue = null)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(@"SELECT s.titlu AS 'Nume Sarcina', COALESCE(p.nume,'') AS 'Proiect Asociat', COALESCE(CONCAT(u.prenume,' ',u.nume),'') AS Responsabil, s.prioritate AS Prioritate, s.status AS Status
FROM sarcini s
JOIN proiecte p ON p.id = s.id_proiect
LEFT JOIN utilizatori u ON u.id = s.id_responsabil", conn);

        if (string.Equals(filterType, "Prioritate", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(filterValue))
        {
            cmd.CommandText += " WHERE s.prioritate = @filter";
            cmd.Parameters.AddWithValue("@filter", filterValue);
        }
        else if (string.Equals(filterType, "Status", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(filterValue))
        {
            cmd.CommandText += " WHERE s.status = @filter";
            cmd.Parameters.AddWithValue("@filter", filterValue);
        }

        cmd.CommandText += " ORDER BY s.data_termen IS NULL, s.data_termen";
        using var adapter = new MySqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }
    public List<Sarcina> GetByProiect(int idProiect) => Query(SelectSql + " WHERE s.id_proiect=@id", ps => ps.AddWithValue("@id", idProiect));
    public List<Sarcina> GetByStatus(string status) => Query(SelectSql + " WHERE s.status=@status", ps => ps.AddWithValue("@status", status));
    public List<Sarcina> GetByResponsabil(int idUser) => Query(SelectSql + " WHERE s.id_responsabil=@id", ps => ps.AddWithValue("@id", idUser));

    public int Insert(Sarcina s)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO sarcini (id_proiect,id_responsabil,titlu,descriere,prioritate,status,data_start,data_termen)
VALUES (@proiect,@responsabil,@titlu,@descriere,@prioritate,@status,@start,@termen); SELECT LAST_INSERT_ID();";
        Add(cmd, s);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Update(Sarcina s)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE sarcini SET id_proiect=@proiect,id_responsabil=@responsabil,titlu=@titlu,descriere=@descriere,
prioritate=@prioritate,status=@status,data_start=@start,data_termen=@termen WHERE id=@id";
        Add(cmd, s);
        cmd.Parameters.AddWithValue("@id", s.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool UpdateStatus(int id, string status)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sarcini SET status=@status WHERE id=@id";
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sarcini WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void Add(MySqlCommand cmd, Sarcina s)
    {
        cmd.Parameters.AddWithValue("@proiect", s.IdProiect);
        cmd.Parameters.AddWithValue("@responsabil", (object?)s.IdResponsabil ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@titlu", s.Titlu);
        cmd.Parameters.AddWithValue("@descriere", s.Descriere);
        cmd.Parameters.AddWithValue("@prioritate", s.Prioritate);
        cmd.Parameters.AddWithValue("@status", s.Status);
        cmd.Parameters.AddWithValue("@start", (object?)s.DataStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@termen", (object?)s.DataTermen ?? DBNull.Value);
    }

    private static List<Sarcina> Query(string sql, Action<MySqlParameterCollection>? parameters = null)
    {
        var list = new List<Sarcina>();
        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(sql, conn);
        parameters?.Invoke(cmd.Parameters);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new Sarcina
        {
            Id = r.GetInt32(0), IdProiect = r.GetInt32(1), NumeProiect = r.GetString(2),
            IdResponsabil = r.IsDBNull(3) ? null : r.GetInt32(3), NumeResponsabil = r.GetString(4),
            Titlu = r.GetString(5), Descriere = r.GetString(6), Prioritate = r.GetString(7), Status = r.GetString(8),
            DataTermen = r.IsDBNull(9) ? null : r.GetDateTime(9)
        });
        return list;
    }
}
