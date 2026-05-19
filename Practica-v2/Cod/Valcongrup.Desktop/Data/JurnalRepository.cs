using System.Data;

namespace Valcongrup.Data;

public class JurnalRepository
{
    public void Log(int idUser, int? idProiect, string actiune, string? tipEntitate = null, int? idEntitate = null)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO jurnal_activitate (id_utilizator, id_proiect, actiune, tip_entitate, id_entitate)
VALUES (@idUser, @idProiect, @actiune, @tip, @idEntitate)";
        cmd.Parameters.AddWithValue("@idUser", idUser);
        cmd.Parameters.AddWithValue("@idProiect", (object?)idProiect ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actiune", actiune);
        cmd.Parameters.AddWithValue("@tip", (object?)tipEntitate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@idEntitate", (object?)idEntitate ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public DataTable GetRecent(int limit = 10) => RepositoryHelpers.Fill(@"
SELECT ja.actiune, ja.creat_la, CONCAT(u.prenume,' ',u.nume) AS utilizator, COALESCE(p.nume,'') AS proiect
FROM jurnal_activitate ja
JOIN utilizatori u ON ja.id_utilizator = u.id AND u.activ = 1
LEFT JOIN proiecte p ON ja.id_proiect = p.id
WHERE LOWER(ja.actiune) NOT LIKE '%sters%'
  AND LOWER(ja.actiune) NOT LIKE '%șters%'
  AND LOWER(ja.actiune) NOT LIKE '%stears%'
ORDER BY ja.creat_la DESC LIMIT @limit", ps => ps.AddWithValue("@limit", limit));

    public DataTable GetNotifications(int limit = 8) => RepositoryHelpers.Fill(@"
SELECT ja.actiune, ja.creat_la, CONCAT(u.prenume,' ',u.nume) AS utilizator, COALESCE(p.nume,'') AS proiect
FROM jurnal_activitate ja
LEFT JOIN utilizatori u ON ja.id_utilizator = u.id
LEFT JOIN proiecte p ON ja.id_proiect = p.id
WHERE LOWER(ja.actiune) LIKE '%cerer%'
   OR LOWER(ja.actiune) LIKE '%sarcin%'
   OR LOWER(ja.actiune) LIKE '%alocat%'
   OR LOWER(ja.actiune) LIKE '%proiect%'
ORDER BY ja.creat_la DESC LIMIT @limit", ps => ps.AddWithValue("@limit", limit));
}
