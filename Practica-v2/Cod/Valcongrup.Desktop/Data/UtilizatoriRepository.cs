using System.Data;
using MySql.Data.MySqlClient;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class UtilizatoriRepository
{
    private static bool _approvalColumnChecked;
    private static bool _companyColumnChecked;

    public Utilizator? Login(string email, string parolaHash)
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT u.id,u.nume,u.prenume,u.email,COALESCE(u.telefon,''),u.id_rol,COALESCE(r.nume,''),u.activ,u.is_approved
FROM utilizatori u LEFT JOIN roluri r ON r.id=u.id_rol
WHERE u.email=@email AND u.parola_hash=@hash AND u.activ=1 AND u.is_approved=1 LIMIT 1";
        cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@hash", parolaHash);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public bool IsPendingApproval(string email, string parolaHash)
    {
        EnsureApprovalColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(1)
FROM utilizatori
WHERE email=@email AND parola_hash=@hash AND activ=1 AND is_approved=0";
        cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@hash", parolaHash);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public List<Utilizator> GetAll()
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        var list = new List<Utilizator>();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT u.id,u.nume,u.prenume,u.email,COALESCE(u.telefon,''),u.id_rol,COALESCE(r.nume,''),u.activ,u.is_approved,COALESCE(u.company_name,'')
FROM utilizatori u LEFT JOIN roluri r ON r.id=u.id_rol ORDER BY u.activ DESC, u.nume, u.prenume";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<Utilizator> GetByRol(string rol) => GetAll().Where(u => u.NumeRol == rol).ToList();

    public List<Utilizator> GetParticipantsForManager(int managerId)
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        var list = new List<Utilizator>();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT DISTINCT u.id,u.nume,u.prenume,u.email,COALESCE(u.telefon,''),u.id_rol,COALESCE(r.nume,''),u.activ,u.is_approved,COALESCE(u.company_name,'')
FROM proiecte p
JOIN proiect_utilizatori pu ON pu.id_proiect=p.id
JOIN utilizatori u ON u.id=pu.id_utilizator
LEFT JOIN roluri r ON r.id=u.id_rol
WHERE p.id_manager=@manager
ORDER BY u.nume, u.prenume";
        cmd.Parameters.AddWithValue("@manager", managerId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public int Insert(Utilizator u, string parolaHash)
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO utilizatori (nume,prenume,email,telefon,id_rol,parola_hash,activ,is_approved,company_name,creat_la)
VALUES (@nume,@prenume,@email,@telefon,@rol,@hash,@activ,@approved,@company,CURRENT_TIMESTAMP); SELECT LAST_INSERT_ID();";
        Add(cmd, u);
        cmd.Parameters.AddWithValue("@hash", parolaHash);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int RegisterPublic(Utilizator u, string parolaHash)
    {
        EnsureApprovalColumn();
        if (IsAdminRole(u.IdRol))
            throw new UnauthorizedAccessException("Unauthorized: public registration cannot request Admin role.");

        u.Activ = true;
        u.IsApproved = false;
        return Insert(u, parolaHash);
    }

    public bool ExistsByEmail(string email)
    {
        EnsureApprovalColumn();
        if (string.IsNullOrWhiteSpace(email)) return false;
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM utilizatori WHERE LOWER(TRIM(email)) = LOWER(TRIM(@email))";
        cmd.Parameters.AddWithValue("@email", email.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public Utilizator? GetByEmail(string email)
    {
        EnsureApprovalColumn();
        if (string.IsNullOrWhiteSpace(email)) return null;
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT u.id,u.nume,u.prenume,u.email,COALESCE(u.telefon,''),u.id_rol,COALESCE(r.nume,''),u.activ,u.is_approved
FROM utilizatori u LEFT JOIN roluri r ON r.id=u.id_rol
WHERE LOWER(TRIM(u.email)) = LOWER(TRIM(@email)) LIMIT 1";
        cmd.Parameters.AddWithValue("@email", email.Trim());
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public bool ExistsByTelefon(string telefon)
    {
        EnsureApprovalColumn();
        if (string.IsNullOrWhiteSpace(telefon)) return false;
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM utilizatori WHERE TRIM(telefon) = TRIM(@telefon)";
        cmd.Parameters.AddWithValue("@telefon", telefon.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool Update(Utilizator u)
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET nume=@nume,prenume=@prenume,email=@email,telefon=@telefon,id_rol=@rol,activ=@activ,is_approved=@approved,company_name=@company WHERE id=@id";
        Add(cmd, u);
        cmd.Parameters.AddWithValue("@id", u.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool SetActiv(int id, bool activ)
    {
        EnsureApprovalColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET activ=@activ WHERE id=@id";
        cmd.Parameters.AddWithValue("@activ", activ);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM utilizatori WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool UpdateParola(int id, string nouHash)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET parola_hash=@hash WHERE id=@id";
        cmd.Parameters.AddWithValue("@hash", nouHash);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool UpdateParola(int id, string vechiHash, string nouHash)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET parola_hash=@nou WHERE id=@id AND parola_hash=@vechi";
        cmd.Parameters.AddWithValue("@nou", nouHash);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@vechi", vechiHash);
        return cmd.ExecuteNonQuery() > 0;
    }

    public void UpdateUltimaLogare(int id)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET ultima_logare=CURRENT_TIMESTAMP WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public DataTable GetRoluriTable() => RepositoryHelpers.Fill("SELECT id,nume FROM roluri ORDER BY nume");

    public DataTable GetPublicRoluriTable() => RepositoryHelpers.Fill("SELECT id,nume FROM roluri WHERE LOWER(nume) <> 'admin' ORDER BY nume");

    public bool IsAdminRole(int idRol)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM roluri WHERE id=@id AND LOWER(nume)='admin'";
        cmd.Parameters.AddWithValue("@id", idRol);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool IsClientRole(int idRol)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM roluri WHERE id=@id AND LOWER(nume)='client'";
        cmd.Parameters.AddWithValue("@id", idRol);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool IsSubcontractorRole(int idRol)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM roluri WHERE id=@id AND LOWER(nume) IN ('subcontractor','subcontractant')";
        cmd.Parameters.AddWithValue("@id", idRol);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public List<Utilizator> GetPendingApproval()
    {
        EnsureApprovalColumn();
        EnsureCompanyColumn();
        var list = new List<Utilizator>();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT u.id,u.nume,u.prenume,u.email,COALESCE(u.telefon,''),u.id_rol,COALESCE(r.nume,''),u.activ,u.is_approved,COALESCE(u.company_name,'')
FROM utilizatori u LEFT JOIN roluri r ON r.id=u.id_rol
WHERE u.is_approved=0
ORDER BY u.creat_la DESC, u.nume, u.prenume";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public bool ApproveUser(int id)
    {
        EnsureApprovalColumn();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE utilizatori SET is_approved=1, activ=1 WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void Add(MySqlCommand cmd, Utilizator u)
    {
        cmd.Parameters.AddWithValue("@nume", u.Nume);
        cmd.Parameters.AddWithValue("@prenume", u.Prenume);
        cmd.Parameters.AddWithValue("@email", u.Email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@telefon", u.Telefon);
        cmd.Parameters.AddWithValue("@rol", u.IdRol);
        cmd.Parameters.AddWithValue("@activ", u.Activ);
        cmd.Parameters.AddWithValue("@approved", u.IsApproved);
        cmd.Parameters.AddWithValue("@company", u.NumeCompanie ?? string.Empty);
    }

    private static Utilizator Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Nume = r.GetString(1),
        Prenume = r.GetString(2),
        Email = r.GetString(3),
        Telefon = r.GetString(4),
        IdRol = r.GetInt32(5),
        NumeRol = r.GetString(6),
        Activ = r.GetBoolean(7),
        IsApproved = r.GetBoolean(8),
        NumeCompanie = r.FieldCount > 9 ? r.GetString(9) : string.Empty
    };

    private static void EnsureApprovalColumn()
    {
        if (_approvalColumnChecked) return;
        using var conn = DbConnection.GetConnection();
        var columnExists = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT COUNT(1)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'utilizatori'
  AND COLUMN_NAME = 'is_approved'";
            columnExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        if (!columnExists)
        {
            using var add = conn.CreateCommand();
            add.CommandText = @"ALTER TABLE utilizatori
ADD COLUMN is_approved TINYINT(1) NOT NULL DEFAULT 1";
            add.ExecuteNonQuery();

            using var alterDefault = conn.CreateCommand();
            alterDefault.CommandText = @"ALTER TABLE utilizatori
ALTER COLUMN is_approved SET DEFAULT 0";
            alterDefault.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"UPDATE utilizatori u
LEFT JOIN roluri r ON r.id=u.id_rol
SET u.is_approved=1
WHERE u.is_approved=0 AND LOWER(COALESCE(r.nume,''))='admin'";
            cmd.ExecuteNonQuery();
        }
        _approvalColumnChecked = true;
    }

    private static void EnsureCompanyColumn()
    {
        if (_companyColumnChecked) return;
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(1)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'utilizatori'
  AND COLUMN_NAME = 'company_name'";
        var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        if (!exists)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE utilizatori ADD COLUMN company_name VARCHAR(255) NULL DEFAULT NULL";
            alter.ExecuteNonQuery();
        }
        _companyColumnChecked = true;
    }
}
