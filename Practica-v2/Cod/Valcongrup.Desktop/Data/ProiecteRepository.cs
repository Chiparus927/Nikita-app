using System.Data;
using MySql.Data.MySqlClient;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class ProiecteRepository
{
    private static bool _managerNullableChecked;
    private static bool _requestSchemaChecked;

    private const string SelectSql = @"SELECT p.id, p.nume, COALESCE(p.descriere,''), p.id_client, COALESCE(c.nume,'') AS nume_client,
       p.id_manager, COALESCE(CONCAT(u.prenume,' ',u.nume),'') AS nume_manager,
       p.buget_total, p.buget_utilizat, p.data_start, p.data_termen, p.progres, p.status
FROM proiecte p
LEFT JOIN clienti c ON p.id_client = c.id
LEFT JOIN utilizatori u ON p.id_manager = u.id";

    public List<Proiect> GetAll() => Query(SelectSql + " WHERE p.id_manager IS NOT NULL ORDER BY p.creat_la DESC");
    public DataTable LoadProjectsData() => LoadProjectsDataForUser(0, "Admin");

    public DataTable LoadProjectsDataForUser(int userId, string roleName)
    {
        return LoadProjectsDataForUser(userId, roleName, null, null);
    }

    public DataTable LoadProjectsDataForUser(int userId, string roleName, string? statusFilter, string? managerFilter)
    {
        EnsureRequestSchema();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT p.nume AS 'Nume Proiect', COALESCE(c.nume,'') AS Client,
       COALESCE(CONCAT(m.prenume,' ',m.nume),'In asteptare') AS Manager,
       p.buget_total AS 'Buget Total', p.progres AS 'Procent Finalizat',
       p.data_start AS 'Data Start', p.data_termen AS 'Data Limita', p.status AS Status
FROM proiecte p
LEFT JOIN clienti c ON p.id_client=c.id
LEFT JOIN utilizatori m ON m.id=p.id_manager";

        var where = new List<string>();

        if (string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("p.id_manager=@userId");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else if (roleName.Equals("Subcontractor", StringComparison.OrdinalIgnoreCase) ||
                 roleName.Equals("Subcontractant", StringComparison.OrdinalIgnoreCase))
        {
            where.Add(@"(p.requested_by_user_id=@userId
   OR EXISTS (SELECT 1 FROM proiect_utilizatori pu WHERE pu.id_proiect=p.id AND pu.id_utilizator=@userId))");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else if (roleName.Equals("Client", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("p.requested_by_user_id=@userId");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else
        {
            where.Add("(p.id_manager IS NOT NULL OR p.status='Cerere')");
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            where.Add("p.status=@status");
            cmd.Parameters.AddWithValue("@status", statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(managerFilter))
        {
            if (managerFilter == "__none__")
            {
                where.Add("p.id_manager IS NULL");
            }
            else if (int.TryParse(managerFilter, out var managerId))
            {
                where.Add("p.id_manager=@managerFilter");
                cmd.Parameters.AddWithValue("@managerFilter", managerId);
            }
        }

        if (where.Count > 0)
            cmd.CommandText += " WHERE " + string.Join(" AND ", where);

        cmd.CommandText += " ORDER BY p.creat_la DESC";
        using var adapter = new MySqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public DataTable GetProjectStatusesForUser(int userId, string roleName)
    {
        var table = LoadProjectsDataForUser(userId, roleName);
        var result = new DataTable();
        result.Columns.Add("value", typeof(string));
        result.Columns.Add("label", typeof(string));
        result.Rows.Add("", "Toate statusurile");

        foreach (DataRow row in table.Rows)
        {
            var status = Convert.ToString(row["Status"]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(status))
                continue;
            if (result.AsEnumerable().Any(r => string.Equals(Convert.ToString(r["value"]), status, StringComparison.OrdinalIgnoreCase)))
                continue;
            result.Rows.Add(status, status);
        }

        return result;
    }

    public DataTable GetManagersForUser(int userId, string roleName)
    {
        EnsureRequestSchema();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT DISTINCT p.id_manager AS value,
       COALESCE(CONCAT(m.prenume,' ',m.nume),'In asteptare') AS label
FROM proiecte p
LEFT JOIN utilizatori m ON m.id=p.id_manager";

        var where = new List<string>();
        if (string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("p.id_manager=@userId");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else if (roleName.Equals("Subcontractor", StringComparison.OrdinalIgnoreCase) ||
                 roleName.Equals("Subcontractant", StringComparison.OrdinalIgnoreCase))
        {
            where.Add(@"(p.requested_by_user_id=@userId
   OR EXISTS (SELECT 1 FROM proiect_utilizatori pu WHERE pu.id_proiect=p.id AND pu.id_utilizator=@userId))");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else if (roleName.Equals("Client", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("p.requested_by_user_id=@userId");
            cmd.Parameters.AddWithValue("@userId", userId);
        }
        else
        {
            where.Add("(p.id_manager IS NOT NULL OR p.status='Cerere')");
        }

        cmd.CommandText += " WHERE " + string.Join(" AND ", where) + " ORDER BY label";
        using var adapter = new MySqlDataAdapter(cmd);
        var raw = new DataTable();
        adapter.Fill(raw);

        var result = new DataTable();
        result.Columns.Add("value", typeof(string));
        result.Columns.Add("label", typeof(string));
        result.Rows.Add("", "Toti managerii");

        foreach (DataRow row in raw.Rows)
        {
            var value = row["value"] == DBNull.Value ? "__none__" : Convert.ToString(row["value"]) ?? string.Empty;
            var label = Convert.ToString(row["label"]) ?? "In asteptare";
            result.Rows.Add(value, label);
        }

        return result;
    }
    public List<Proiect> GetActive() => Query(SelectSql + " WHERE p.status IN ('Activ','Planificat') ORDER BY p.creat_la DESC");
    public Proiect? GetById(int id) => Query(SelectSql + " WHERE p.id=@id", ps => ps.AddWithValue("@id", id)).FirstOrDefault();

    public int Insert(Proiect p)
    {
        EnsureManagerNullable();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO proiecte (nume, descriere, id_client, id_manager, buget_total, buget_utilizat, data_start, data_termen, progres, status)
VALUES (@nume,@descriere,@client,@manager,@total,@utilizat,@start,@termen,@progres,@status); SELECT LAST_INSERT_ID();";
        Add(cmd, p);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Update(Proiect p)
    {
        EnsureManagerNullable();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE proiecte SET nume=@nume, descriere=@descriere, id_client=@client, id_manager=@manager,
buget_total=@total, buget_utilizat=@utilizat, data_start=@start, data_termen=@termen, progres=@progres, status=@status WHERE id=@id";
        Add(cmd, p);
        cmd.Parameters.AddWithValue("@id", p.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM proiecte WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void Add(MySqlCommand cmd, Proiect p)
    {
        cmd.Parameters.AddWithValue("@nume", p.Nume);
        cmd.Parameters.AddWithValue("@descriere", p.Descriere);
        cmd.Parameters.AddWithValue("@client", (object?)p.IdClient ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@manager", (object?)p.IdManager ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@total", p.BugetTotal);
        cmd.Parameters.AddWithValue("@utilizat", p.BugetUtilizat);
        cmd.Parameters.AddWithValue("@start", p.DataStart.Date);
        cmd.Parameters.AddWithValue("@termen", p.DataTermen.Date);
        cmd.Parameters.AddWithValue("@progres", p.Progres);
        cmd.Parameters.AddWithValue("@status", p.Status);
    }

    private static List<Proiect> Query(string sql, Action<MySqlParameterCollection>? parameters = null)
    {
        var list = new List<Proiect>();
        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(sql, conn);
        parameters?.Invoke(cmd.Parameters);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new Proiect
        {
            Id = r.GetInt32(0), Nume = r.GetString(1), Descriere = r.GetString(2),
            IdClient = r.IsDBNull(3) ? null : r.GetInt32(3), NumeClient = r.GetString(4),
            IdManager = r.IsDBNull(5) ? null : r.GetInt32(5), NumeManager = r.GetString(6),
            BugetTotal = r.GetDecimal(7), BugetUtilizat = r.GetDecimal(8),
            DataStart = r.GetDateTime(9), DataTermen = r.GetDateTime(10), Progres = r.GetInt32(11), Status = r.GetString(12)
        });
        return list;
    }

    public List<Proiect> GetPendingRequests() => Query(SelectSql + " WHERE p.id_manager IS NULL AND p.status='Cerere' ORDER BY p.creat_la DESC");

    public List<Proiect> Search(string keyword)
    {
        var k = $"%{keyword.Trim()}%";
        return Query(SelectSql + " WHERE p.id_manager IS NOT NULL AND (p.nume LIKE @k OR p.descriere LIKE @k OR c.nume LIKE @k) ORDER BY p.creat_la DESC",
            ps => ps.AddWithValue("@k", k));
    }

    public int CreateClientRequest(int clientUserId, string nume, string descriere, decimal bugetEstimat, DateTime termen)
    {
        EnsureRequestSchema();
        var clientId = GetOrCreateClientIdForUser(clientUserId);
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO proiecte (nume, descriere, id_client, id_manager, requested_by_user_id, buget_total, buget_utilizat, data_start, data_termen, progres, status)
VALUES (@nume,@descriere,@client,NULL,@requester,@total,0,CURRENT_DATE,@termen,0,'Cerere'); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@nume", nume);
        cmd.Parameters.AddWithValue("@descriere", descriere);
        cmd.Parameters.AddWithValue("@client", clientId);
        cmd.Parameters.AddWithValue("@requester", clientUserId);
        cmd.Parameters.AddWithValue("@total", bugetEstimat);
        cmd.Parameters.AddWithValue("@termen", termen.Date);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int GetOrCreateClientIdForUser(int userId)
    {
        using var conn = DbConnection.GetConnection();
        string nume;
        string prenume;
        string email;
        string telefon;
        using (var userCmd = conn.CreateCommand())
        {
            userCmd.CommandText = "SELECT nume, prenume, email, COALESCE(telefon,'') FROM utilizatori WHERE id=@id";
            userCmd.Parameters.AddWithValue("@id", userId);
            using var r = userCmd.ExecuteReader();
            if (!r.Read()) throw new InvalidOperationException("Clientul logat nu mai exista in sistem.");
            nume = r.GetString(0);
            prenume = r.GetString(1);
            email = r.GetString(2);
            telefon = r.GetString(3);
        }

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var colCmd = conn.CreateCommand())
        {
            colCmd.CommandText = @"SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='clienti'";
            using var r = colCmd.ExecuteReader();
            while (r.Read()) columns.Add(r.GetString(0));
        }

        if (columns.Contains("email"))
        {
            using var existing = conn.CreateCommand();
            existing.CommandText = "SELECT id FROM clienti WHERE email=@email LIMIT 1";
            existing.Parameters.AddWithValue("@email", email);
            var found = existing.ExecuteScalar();
            if (found != null && found != DBNull.Value) return Convert.ToInt32(found);
        }

        var fullName = $"{prenume} {nume}".Trim();
        using (var existing = conn.CreateCommand())
        {
            existing.CommandText = "SELECT id FROM clienti WHERE nume=@nume LIMIT 1";
            existing.Parameters.AddWithValue("@nume", fullName);
            var found = existing.ExecuteScalar();
            if (found != null && found != DBNull.Value) return Convert.ToInt32(found);
        }

        var names = new List<string> { "nume" };
        var values = new List<string> { "@nume" };
        using var insert = conn.CreateCommand();
        insert.Parameters.AddWithValue("@nume", fullName);
        if (columns.Contains("email"))
        {
            names.Add("email");
            values.Add("@email");
            insert.Parameters.AddWithValue("@email", email);
        }
        if (columns.Contains("telefon"))
        {
            names.Add("telefon");
            values.Add("@telefon");
            insert.Parameters.AddWithValue("@telefon", telefon);
        }
        insert.CommandText = $"INSERT INTO clienti ({string.Join(",", names)}) VALUES ({string.Join(",", values)}); SELECT LAST_INSERT_ID();";
        return Convert.ToInt32(insert.ExecuteScalar());
    }

    public bool AcceptRequest(int projectId, int managerId)
    {
        EnsureRequestSchema();
        using var conn = DbConnection.GetConnection();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE proiecte SET id_manager=@manager, status='Activ' WHERE id=@id AND id_manager IS NULL";
        cmd.Parameters.AddWithValue("@manager", managerId);
        cmd.Parameters.AddWithValue("@id", projectId);
        var updated = cmd.ExecuteNonQuery() > 0;

        if (updated)
        {
            using var participant = conn.CreateCommand();
            participant.Transaction = tx;
            participant.CommandText = @"INSERT IGNORE INTO proiect_utilizatori (id_proiect, id_utilizator)
SELECT p.id, p.requested_by_user_id
FROM proiecte p
JOIN utilizatori u ON u.id=p.requested_by_user_id
JOIN roluri r ON r.id=u.id_rol
WHERE p.id=@id AND LOWER(r.nume) IN ('subcontractor','subcontractant')";
            participant.Parameters.AddWithValue("@id", projectId);
            participant.ExecuteNonQuery();
        }

        tx.Commit();
        return updated;
    }

    public bool RejectRequest(int projectId)
    {
        EnsureRequestSchema();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE proiecte SET status='Refuzat' WHERE id=@id AND id_manager IS NULL";
        cmd.Parameters.AddWithValue("@id", projectId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void EnsureRequestSchema()
    {
        if (_requestSchemaChecked) return;
        EnsureManagerNullable();
        using var conn = DbConnection.GetConnection();

        using (var statusCmd = conn.CreateCommand())
        {
            statusCmd.CommandText = @"SELECT COLUMN_TYPE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='proiecte' AND COLUMN_NAME='status'";
            var statusType = Convert.ToString(statusCmd.ExecuteScalar()) ?? string.Empty;
            if (!statusType.Contains("'Cerere'", StringComparison.OrdinalIgnoreCase) ||
                !statusType.Contains("'Refuzat'", StringComparison.OrdinalIgnoreCase))
            {
                using var alterStatus = conn.CreateCommand();
                alterStatus.CommandText = "ALTER TABLE proiecte MODIFY COLUMN status ENUM('Planificat','Activ','Suspendat','Finalizat','Cerere','Refuzat') NOT NULL DEFAULT 'Planificat'";
                alterStatus.ExecuteNonQuery();
            }
        }

        using (var colCmd = conn.CreateCommand())
        {
            colCmd.CommandText = @"SELECT COUNT(1)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='proiecte' AND COLUMN_NAME='requested_by_user_id'";
            var exists = Convert.ToInt32(colCmd.ExecuteScalar()) > 0;
            if (!exists)
            {
                var userIdType = GetColumnType(conn, "utilizatori", "id");
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE proiecte ADD COLUMN requested_by_user_id {userIdType} NULL";
                alter.ExecuteNonQuery();

                using var index = conn.CreateCommand();
                index.CommandText = "ALTER TABLE proiecte ADD INDEX idx_proiecte_requested_by (requested_by_user_id)";
                index.ExecuteNonQuery();

                using var fk = conn.CreateCommand();
                fk.CommandText = "ALTER TABLE proiecte ADD CONSTRAINT fk_proiecte_requested_by FOREIGN KEY (requested_by_user_id) REFERENCES utilizatori(id)";
                fk.ExecuteNonQuery();
            }
        }

        _requestSchemaChecked = true;
    }

    private static void EnsureManagerNullable()
    {
        if (_managerNullableChecked) return;
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT IS_NULLABLE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME='proiecte' AND COLUMN_NAME='id_manager'";
        var nullable = Convert.ToString(cmd.ExecuteScalar());
        if (string.Equals(nullable, "NO", StringComparison.OrdinalIgnoreCase))
        {
            var managerType = GetColumnType(conn, "utilizatori", "id");
            var fkName = GetForeignKeyConstraintName(conn, "proiecte", "id_manager");
            if (!string.IsNullOrEmpty(fkName))
            {
                using var dropFk = conn.CreateCommand();
                dropFk.CommandText = $"ALTER TABLE proiecte DROP FOREIGN KEY `{fkName}`";
                dropFk.ExecuteNonQuery();
            }

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE proiecte MODIFY COLUMN id_manager {managerType} NULL";
            alter.ExecuteNonQuery();

            if (!string.IsNullOrEmpty(fkName))
            {
                using var addFk = conn.CreateCommand();
                addFk.CommandText = $"ALTER TABLE proiecte ADD CONSTRAINT `{fkName}` FOREIGN KEY (id_manager) REFERENCES utilizatori(id)";
                addFk.ExecuteNonQuery();
            }
        }
        _managerNullableChecked = true;
    }

    private static string GetColumnType(MySqlConnection conn, string tableName, string columnName)
    {
        using var typeCmd = conn.CreateCommand();
        typeCmd.CommandText = @"SELECT COLUMN_TYPE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME=@table AND COLUMN_NAME=@column";
        typeCmd.Parameters.AddWithValue("@table", tableName);
        typeCmd.Parameters.AddWithValue("@column", columnName);
        return Convert.ToString(typeCmd.ExecuteScalar()) ?? "int";
    }

    private static string? GetForeignKeyConstraintName(MySqlConnection conn, string tableName, string columnName)
    {
        using var fkCmd = conn.CreateCommand();
        fkCmd.CommandText = @"SELECT CONSTRAINT_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column AND REFERENCED_TABLE_NAME IS NOT NULL";
        fkCmd.Parameters.AddWithValue("@table", tableName);
        fkCmd.Parameters.AddWithValue("@column", columnName);
        return Convert.ToString(fkCmd.ExecuteScalar());
    }
}

