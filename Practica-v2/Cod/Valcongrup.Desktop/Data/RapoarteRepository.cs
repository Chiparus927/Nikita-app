using System.Data;
using Valcongrup.Data;

namespace Valcongrup.Data;

public record EvolutieLunaraRow(string Label, decimal Consumat, decimal Estimat);
public record CategorieRow(string Nume, decimal Total, int NrTranzactii);

public class RapoarteRepository
{
    private static readonly string[] MonthNames =
        { "", "Ian", "Feb", "Mar", "Apr", "Mai", "Iun", "Iul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    // ── existing (kept for backward compat) ──────────────────────────────
    public DataTable GetDistributieGlobala()
    {
        const string sql = @"SELECT cb.nume, SUM(b.suma) as total
                             FROM buget_tranzactii b
                             JOIN categorii_buget cb ON b.id_categorie = cb.id
                             GROUP BY cb.id, cb.nume ORDER BY total DESC";
        return RepositoryHelpers.Fill(sql);
    }

    public DataTable GetEvolutieBuget()
    {
        const string sql = @"SELECT MONTH(data_tranzactie) as luna,
                                    YEAR(data_tranzactie)  as anul,
                                    SUM(suma) as total
                             FROM buget_tranzactii
                             GROUP BY YEAR(data_tranzactie), MONTH(data_tranzactie)
                             ORDER BY anul ASC, luna ASC LIMIT 12";
        return RepositoryHelpers.Fill(sql);
    }

    // ── new: comparative line-chart series ───────────────────────────────
    public List<EvolutieLunaraRow> GetEvolutieComparativa()
    {
        var consumat = new Dictionary<string, decimal>();
        var estimat  = new Dictionary<string, decimal>();

        using (var conn = DbConnection.GetConnection())
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT YEAR(data_tranzactie), MONTH(data_tranzactie), SUM(suma)
                                FROM buget_tranzactii
                                GROUP BY YEAR(data_tranzactie), MONTH(data_tranzactie)
                                ORDER BY 1, 2 LIMIT 12";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var key = $"{MonthNames[r.GetInt32(1)]} {r.GetInt32(0)}";
                consumat[key] = r.GetDecimal(2);
            }
        }

        using (var conn = DbConnection.GetConnection())
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT YEAR(data_start), MONTH(data_start), SUM(buget_total)
                                FROM proiecte
                                WHERE data_start IS NOT NULL AND status != 'Cerere'
                                GROUP BY YEAR(data_start), MONTH(data_start)
                                ORDER BY 1, 2 LIMIT 12";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var key = $"{MonthNames[r.GetInt32(1)]} {r.GetInt32(0)}";
                estimat[key] = r.GetDecimal(2);
            }
        }

        var keys = consumat.Keys.Union(estimat.Keys).Distinct().ToList();
        return keys.Select(k => new EvolutieLunaraRow(
            k,
            consumat.TryGetValue(k, out var c) ? c : 0m,
            estimat.TryGetValue(k,  out var e) ? e : 0m
        )).ToList();
    }

    // ── new: pie / donut breakdown ────────────────────────────────────────
    public List<CategorieRow> GetDistributieFull()
    {
        var result = new List<CategorieRow>();
        using var conn = DbConnection.GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"SELECT cb.nume, SUM(b.suma) as total, COUNT(*) as nr
                            FROM buget_tranzactii b
                            JOIN categorii_buget cb ON b.id_categorie = cb.id
                            GROUP BY cb.id, cb.nume
                            ORDER BY total DESC";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            result.Add(new CategorieRow(r.GetString(0), r.GetDecimal(1), r.GetInt32(2)));
        return result;
    }
}
