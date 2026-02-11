using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace SampleApp2_SetData
{
    public partial class Form1 : Form
    {
        private Majorsilence.Reporting.RdlViewer.RdlViewer rdlViewerSourceRdlNoLoad;
        private Majorsilence.Reporting.RdlViewer.RdlViewer rdlViewerSetSource;

        public Form1()
        {
            InitializeComponent();
            var split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            this.Controls.Add(split);

            rdlViewerSourceRdlNoLoad = new Majorsilence.Reporting.RdlViewer.RdlViewer();
            rdlViewerSourceRdlNoLoad.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(rdlViewerSourceRdlNoLoad);
            rdlViewerSetSource = new Majorsilence.Reporting.RdlViewer.RdlViewer();
            rdlViewerSetSource.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(rdlViewerSetSource);
            
            // 50% left, 50% right
            split.SplitterDistance = split.Width / 2;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // TODO: You must change this connection string to match where your database is
            string sqlFile = System.IO.Path.Combine(AppContext.BaseDirectory, @"..\", @"..\", @"..\",
                @"..\", @"..\", "northwindEF.db");
            string connectionString = $"Data Source={sqlFile}";

            using SqliteConnection cn = new SqliteConnection(connectionString);
            using SqliteCommand cmd = new SqliteCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT CategoryID, CategoryName, Description FROM Categories;";
            cmd.Connection = cn;
            DataTable dt = await GetTable(cmd);

            string filepath =
                System.IO.Path.Combine(AppContext.BaseDirectory, "SampleApp2-TestReport.rdl");

            await ExampleViaSourceRdlNoLoad(filepath, dt);
            await ExampleViaSetSourceFile(filepath, dt);
        }

        private async Task ExampleViaSourceRdlNoLoad(string filepath, DataTable dt)
        {
            rdlViewerSourceRdlNoLoad.SourceRdlNoLoad = await System.IO.File.ReadAllTextAsync(filepath);
            rdlViewerSourceRdlNoLoad.Parameters = "";
            var rpt = await rdlViewerSourceRdlNoLoad.Report();
            await rpt.DataSets["Data"].SetData(dt);
            await rdlViewerSourceRdlNoLoad.Rebuild();
        }

        private async Task ExampleViaSetSourceFile(string filepath, DataTable dt)
        {
            await rdlViewerSetSource.SetSourceFile(new Uri(filepath));
            var rpt = await rdlViewerSetSource.Report();
            await rpt.DataSets["Data"].SetData(dt);
            await rdlViewerSetSource.Rebuild();
        }


        private async Task<DataTable> GetTable(SqliteCommand cmd)
        {
            System.Data.ConnectionState original = cmd.Connection.State;
            if (cmd.Connection.State == ConnectionState.Closed)
            {
                await cmd.Connection.OpenAsync();
            }

            DataTable dt = new DataTable();
            await using var dr = await cmd.ExecuteReaderAsync();
            dt.Load(dr);
            dr.Close();

            if (original == ConnectionState.Closed)
            {
                cmd.Connection.Close();
            }

            return dt;
        }
    }
}