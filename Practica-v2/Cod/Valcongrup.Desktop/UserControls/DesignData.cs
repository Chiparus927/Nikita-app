using System.Data;

namespace Valcongrup.UserControls;

internal static class DesignData
{
    public static DataTable Projects(bool includeCompleted)
    {
        var dt = new DataTable();
        dt.Columns.Add("Nume Proiect");
        dt.Columns.Add("Client");
        dt.Columns.Add("Buget Total");
        dt.Columns.Add("Procent Finalizat");
        dt.Columns.Add("Data Start");
        dt.Columns.Add("Data Limită");
        dt.Columns.Add("Status");
        dt.Rows.Add("Școala Generală nr.5", "Ministerul Educației", "850,000 MDL", "40%", "01 Mar 2025", "30 Jun 2026", "Activ");
        dt.Rows.Add("Centru Comercial Nord", "SC Construct SRL", "5,000,000 MDL", "10%", "01 Jun 2025", "30 Jun 2027", "Planificat");
        if (includeCompleted)
            dt.Rows.Add("Parcare Subterană", "Primăria Chișinău", "1,200,000 MDL", "100%", "01 Jan 2024", "31 Dec 2024", "Finalizat");
        return dt;
    }

    public static DataTable Tasks()
    {
        var dt = new DataTable();
        dt.Columns.Add("Nume Sarcină");
        dt.Columns.Add("Proiect Asociat");
        dt.Columns.Add("Responsabil");
        dt.Columns.Add("Prioritate");
        dt.Columns.Add("Status");
        dt.Columns.Add("Acțiuni");
        dt.Rows.Add("Proiect renovare", "Școala Generală nr.5", "Andrei Rusu", "Scăzută", "Finalizată", "...");
        dt.Rows.Add("Demolare pereți vechi", "Școala Generală nr.5", "Maria Ionescu", "Ridicată", "Finalizată", "...");
        dt.Rows.Add("Studiu de fezabilitate", "Centru Comercial Nord", "Andrei Rusu", "Medie", "În Progres", "...");
        dt.Rows.Add("Renovare săli de clasă", "Școala Generală nr.5", "Maria Ionescu", "Medie", "În Progres", "...");
        dt.Rows.Add("Autorizație construcție", "Centru Comercial Nord", "Maria Ionescu", "Urgentă", "În Așteptare", "...");
        return dt;
    }

    public static DataTable Documents()
    {
        var dt = new DataTable();
        dt.Columns.Add("id");
        dt.Columns.Add("Nume Fișier");
        dt.Columns.Add("Format");
        dt.Columns.Add("Categorie");
        dt.Columns.Add("Proiect");
        dt.Columns.Add("Dimensiune (KB)");
        dt.Columns.Add("Data Încărcare");
        dt.Columns.Add("Încărcat de");
        dt.Columns.Add("cale_fișier");
        dt.Rows.Add("4", "Contract_Scoala5.pdf", "PDF", "Contracte", "Școala Generală nr.5", "980", "5/12/2026 11:41 AM", "Ion Popescu", "./Documente/Contra...");
        dt.Rows.Add("5", "Autorizație_Scoala5.pdf", "PDF", "Autorizații", "Școala Generală nr.5", "450", "5/12/2026 11:41 AM", "Ion Popescu", "./Documente/Autori...");
        dt.Rows.Add("6", "Planse_CCNord_draft.dwg", "DWG", "Planșe Arhitectură", "Centru Comercial Nord", "12000", "5/12/2026 11:41 AM", "Andrei Rusu", "./Documente/Planse...");
        return dt;
    }

    public static DataTable CalendarEvents()
    {
        var dt = new DataTable();
        dt.Columns.Add("id");
        dt.Columns.Add("Titlu");
        dt.Columns.Add("Tip");
        dt.Columns.Add("Proiect");
        dt.Columns.Add("Start");
        dt.Columns.Add("Sfârșit");
        return dt;
    }
}
