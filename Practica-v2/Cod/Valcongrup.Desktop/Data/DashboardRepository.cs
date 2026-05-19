using System.Data;
using MySql.Data.MySqlClient;

namespace Valcongrup.Data;

public class DashboardRepository
{
    public DashboardKpis GetKpis()
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(@"
SELECT COUNT(*) FROM proiecte WHERE status = 'Activ';
SELECT COUNT(*) FROM sarcini
WHERE status NOT IN ('Finalizata','Finalizat','Anulata','Anulat');
SELECT COUNT(*) FROM utilizatori WHERE activ = 1;
SELECT COALESCE(SUM(buget_total),0) FROM proiecte;", conn);

        using var r = cmd.ExecuteReader();
        var kpis = new DashboardKpis();
        if (r.Read()) kpis.ActiveProjectsCount = r.GetInt32(0);
        if (r.NextResult() && r.Read()) kpis.PendingTasksCount = r.GetInt32(0);
        if (r.NextResult() && r.Read()) kpis.TeamMembersCount = r.GetInt32(0);
        if (r.NextResult() && r.Read()) kpis.TotalUtilizedBudget = r.GetDecimal(0);
        return kpis;
    }

    public int CountProiecteActive() => GetKpis().ActiveProjectsCount;
    public int CountSarciniInProgres() => GetKpis().PendingTasksCount;
    public decimal BugetUtilizat() => GetKpis().TotalUtilizedBudget;
    public int CountMembri() => GetKpis().TeamMembersCount;

    public DataTable ActiveProjects() => RepositoryHelpers.Fill(@"
SELECT p.nume AS Proiect,
       COALESCE(c.nume,'') AS Client,
       COALESCE(CONCAT(u.prenume,' ',u.nume),'') AS Manager,
       p.progres AS Progres,
       p.status AS Status,
       p.data_termen AS Termen
FROM proiecte p
LEFT JOIN clienti c ON p.id_client=c.id
LEFT JOIN utilizatori u ON p.id_manager=u.id
WHERE p.status IN ('Activ','Planificat')
ORDER BY p.data_termen");

    public List<WeeklyProjectProgress> GetWeeklyProjectProgress(DateTime today)
    {
        var monday = today.Date.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var sunday = monday.AddDays(6);
        var rows = Enumerable.Range(0, 7)
            .Select(i => new WeeklyProjectProgress(i, 0, 0))
            .ToArray();

        using var conn = DbConnection.GetConnection();
        using var cmd = new MySqlCommand(@"
SELECT ((DAYOFWEEK(activity_date) + 5) % 7) AS day_index,
       SUM(CASE WHEN status = 'Finalizat' THEN 1 ELSE 0 END) AS finalizate,
       SUM(CASE WHEN status IN ('Activ','Planificat','Suspendat') THEN 1 ELSE 0 END) AS in_lucru
FROM (
    SELECT status, DATE(COALESCE(actualizat_la, creat_la)) AS activity_date
    FROM proiecte
    WHERE status <> 'Refuzat'
) p
WHERE activity_date BETWEEN @start AND @end
GROUP BY day_index
ORDER BY day_index;", conn);
        cmd.Parameters.AddWithValue("@start", monday);
        cmd.Parameters.AddWithValue("@end", sunday);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var index = r.GetInt32(0);
            if (index < 0 || index >= rows.Length)
                continue;

            rows[index] = new WeeklyProjectProgress(
                index,
                r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetDecimal(1)),
                r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetDecimal(2)));
        }

        return rows.ToList();
    }
}

public sealed class DashboardKpis
{
    public int ActiveProjectsCount { get; set; }
    public int PendingTasksCount { get; set; }
    public int TeamMembersCount { get; set; }
    public decimal TotalUtilizedBudget { get; set; }
    public string TotalUtilizedBudgetMdl => $"{TotalUtilizedBudget:N0} MDL";
}

public sealed record WeeklyProjectProgress(int DayIndex, int Finalizate, int InLucru);
