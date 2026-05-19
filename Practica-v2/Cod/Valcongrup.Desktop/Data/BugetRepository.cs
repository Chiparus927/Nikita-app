using System.Data;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class BugetRepository
{
    public List<TranzactieBuget> GetByProiect(int idProiect)
    {
        var list = new List<TranzactieBuget>();
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT b.id,b.id_proiect,b.id_categorie,COALESCE(cb.nume,''),b.suma,COALESCE(b.descriere,''),b.data_tranzactie
FROM buget_tranzactii b LEFT JOIN categorii_buget cb ON cb.id=b.id_categorie
WHERE b.id_proiect=@id ORDER BY b.data_tranzactie DESC";
        cmd.Parameters.AddWithValue("@id", idProiect);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new TranzactieBuget { Id = r.GetInt32(0), IdProiect = r.GetInt32(1), IdCategorie = r.IsDBNull(2) ? null : r.GetInt32(2), Categorie = r.GetString(3), Suma = r.GetDecimal(4), Descriere = r.GetString(5), DataTranzactie = r.GetDateTime(6) });
        return list;
    }

    public int Insert(TranzactieBuget t)
    {
        using var conn = DbConnection.GetConnection();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"INSERT INTO buget_tranzactii (id_proiect,id_categorie,suma,descriere,data_tranzactie)
VALUES (@p,@c,@s,@d,@dt); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@p", t.IdProiect);
        cmd.Parameters.AddWithValue("@c", (object?)t.IdCategorie ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@s", t.Suma);
        cmd.Parameters.AddWithValue("@d", t.Descriere);
        cmd.Parameters.AddWithValue("@dt", t.DataTranzactie.Date);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        using var upd = conn.CreateCommand();
        upd.Transaction = tx;
        upd.CommandText = "UPDATE proiecte SET buget_utilizat=buget_utilizat+@s WHERE id=@p";
        upd.Parameters.AddWithValue("@s", t.Suma);
        upd.Parameters.AddWithValue("@p", t.IdProiect);
        upd.ExecuteNonQuery();
        tx.Commit();
        return id;
    }

    public bool Delete(int id)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM buget_tranzactii WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public decimal GetBugetRamas(int idProiect) => Convert.ToDecimal(RepositoryHelpers.Fill("SELECT COALESCE(buget_total-buget_utilizat,0) FROM proiecte WHERE id=@id", ps => ps.AddWithValue("@id", idProiect)).Rows[0][0]);
    public DataTable GetDistributiePeCategorie(int idProiect) => RepositoryHelpers.Fill("SELECT * FROM v_distributie_buget WHERE id_proiect=@id", ps => ps.AddWithValue("@id", idProiect));
    public DataTable GetCategorii() => RepositoryHelpers.Fill("SELECT id,nume FROM categorii_buget ORDER BY nume");
}
