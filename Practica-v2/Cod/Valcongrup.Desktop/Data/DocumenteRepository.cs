using System.Data;
using Valcongrup.Models;

namespace Valcongrup.Data;

public class DocumenteRepository
{
    public DataTable GetAll() => RepositoryHelpers.Fill(@"SELECT d.id, d.nume_fisier AS 'Nume Fisier', d.format AS Format, COALESCE(cd.nume,'') AS Categorie, COALESCE(p.nume,'') AS Proiect, d.dimensiune_kb AS 'Dimensiune (KB)', d.data_incarcare AS 'Data Incarcare', COALESCE(CONCAT(u.prenume,' ',u.nume),'') AS 'Incarcat de', d.cale_fisier FROM documente d LEFT JOIN categorii_documente cd ON cd.id=d.id_categorie LEFT JOIN proiecte p ON p.id=d.id_proiect LEFT JOIN utilizatori u ON u.id=d.id_incarcat_de ORDER BY d.data_incarcare DESC");

    public int Insert(Document d)
    {
        using var conn = DbConnection.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO documente (id_proiect,id_categorie,id_incarcat_de,nume_fisier,cale_fisier,format,dimensiune_kb,data_incarcare)
VALUES (@p,@c,@u,@n,@cale,@f,@kb,CURRENT_TIMESTAMP); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@p", d.IdProiect);
        cmd.Parameters.AddWithValue("@c", d.IdCategorie);
        cmd.Parameters.AddWithValue("@u", d.IdIncarcatDe);
        cmd.Parameters.AddWithValue("@n", d.NumeFisier);
        cmd.Parameters.AddWithValue("@cale", d.CaleFisier);
        cmd.Parameters.AddWithValue("@f", d.Format);
        cmd.Parameters.AddWithValue("@kb", d.DimensiuneKb);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public DataTable GetCategorii() => RepositoryHelpers.Fill("SELECT id,nume FROM categorii_documente ORDER BY nume");
}
