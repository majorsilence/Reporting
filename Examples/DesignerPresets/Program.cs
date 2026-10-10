using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign;

// Starts the report designer with presets for the New Report wizard (File > New).
// Every new report gets the connection and the parameters below, so they are available
// while designing and are saved into the .rdl. A viewer can then pass values by name, e.g.
//   viewer.Parameters = "Country=Canada&MinOrders=2";
namespace DesignerPresets
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetDefaultFont(SystemFonts.MessageBoxFont);

            var designer = new RdlDesigner("DesignerPresetsExample", false);

            // Predefined connection; hide the tab so end users never see the connection string.
            designer.NewReportConnectionType = DialogDatabase.ConnectionType.SQLITE;
            designer.NewReportConnectionString =
                "Data Source=" + Path.Combine(AppContext.BaseDirectory, "sqlitetestdb2.db");
            designer.NewReportHideConnectionTab = true;

            // Default parameters, chosen per kind of report.
            designer.NewReportParameters.Add(new DefaultReportParameter
            {
                Name = "Country",
                DataType = "String",
                Prompt = "Country",
                DefaultValue = "USA",
            });
            designer.NewReportParameters.Add(new DefaultReportParameter
            {
                Name = "MinOrders",
                DataType = "Integer",
                Prompt = "Minimum orders",
                DefaultValue = "0",
            });

            Application.Run(designer);
        }
    }
}
