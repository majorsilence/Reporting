using Majorsilence.Reporting.Rdl;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
    public class Create
    {
        public async Task<Rdl.Report> GenerateRdl(Report report)
        {
            var serializer = new XmlSerializer(typeof(Report));
            string xml;
            await using (var writer = new Utf8StringWriter())
            {
                serializer.Serialize(writer, report);
                xml = writer.ToString();
            }

            var rdlp = new RDLParser(xml);
            var fyiReport = await rdlp.Parse();

            ValidateReport(fyiReport);

            return fyiReport;
        }

        private static void ValidateReport(Rdl.Report fyiReport)
        {
            if (fyiReport.ErrorMaxSeverity > 0)
            {
                if (fyiReport.ErrorMaxSeverity > 4)
                {
                    var errorMessages = string.Join(Environment.NewLine, fyiReport.ErrorItems.Cast<string>());
                    int severity = fyiReport.ErrorMaxSeverity;
                    fyiReport.ErrorReset();
                    throw new Exception($"Errors encountered with severity {severity}:{Environment.NewLine}{errorMessages}");
                }
            }
        }

        public async Task<Rdl.Report> GenerateRdl(DataTable data,
            string description = "",
            string author = "",
            string pageHeight = "11in",
            string pageWidth = "8.5in",
            string width = "7.5in",
            string topMargin = ".25in",
            string leftMargin = ".25in",
            string rightMargin = ".25in",
            string bottomMargin = ".25in",
            string pageHeaderText = "",
            string name="")
        {
            var headerTableCells = new List<TableCell>();
            var bodyTableCells = new List<TableCell>();
            var fields = new List<Field>();

            for (int i = 0; i < data.Columns.Count; i++)
            {
                string colName = data.Columns[i].ColumnName;
                TypeCode colType = Type.GetTypeCode(data.Columns[i].DataType);
                headerTableCells.Add(new TableCell
                {
                    ReportItems = new TableCellReportItems()
                    {
                        ReportItem = new Text
                        {
                            Name = $"TextboxH{colName}",
                            Value = new Value { Text = colName },
                            Style = new Style { TextAlign = "Center", FontWeight = "Bold" }
                        }
                    }
                });

                bodyTableCells.Add(new TableCell
                {
                    ReportItems = new TableCellReportItems()
                    {
                        ReportItem = new Text
                        {
                            Name = $"TextBoxB{colName}",
                            Value = new Value { Text = $"=Fields!{colName}.Value" },
                            CanGrow = true
                        }
                    }
                });

                fields.Add(new Field
                {
                    Name = colName,
                    DataField = colName,
                    TypeName = colType.ToString()
                });
            }

            var xml = InternalReportCreation("", "",
                "", description, author, pageHeight, pageWidth, width, topMargin, leftMargin,
                rightMargin, bottomMargin, pageHeaderText, headerTableCells, bodyTableCells, fields, name);

            var rdlp = new RDLParser(xml);
            var fyiReport = await rdlp.Parse();
            ValidateReport(fyiReport);
            await fyiReport.DataSets["Data"].SetData(data);
            return fyiReport;
        }

        public async Task<Rdl.Report> GenerateRdl<T>(IEnumerable<T> data,
            string description = "",
            string author = "",
            string pageHeight = "11in",
            string pageWidth = "8.5in",
            string width = "7.5in",
            string topMargin = ".25in",
            string leftMargin = ".25in",
            string rightMargin = ".25in",
            string bottomMargin = ".25in",
            string pageHeaderText = "",
            string name="")
        {
            var headerTableCells = new List<TableCell>();
            var bodyTableCells = new List<TableCell>();
            var fields = new List<Field>();

            var properties = typeof(T).GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                string colName = properties[i].Name;
                TypeCode colType = Type.GetTypeCode(properties[i].PropertyType);
                headerTableCells.Add(new TableCell
                {
                    ReportItems = new TableCellReportItems()
                    {
                        ReportItem = new Text
                        {
                            Name = $"TextboxH{colName}",
                            Value = new Value { Text = colName },
                            Style = new Style { TextAlign = "Center", FontWeight = "Bold" }
                        }
                    }
                });

                bodyTableCells.Add(new TableCell
                {
                    ReportItems = new TableCellReportItems()
                    {
                        ReportItem = new Text
                        {
                            Name = $"TextBoxB{colName}",
                            Value = new Value { Text = $"=Fields!{colName}.Value" },
                            CanGrow = true
                        }
                    }
                });

                fields.Add(new Field
                {
                    Name = colName,
                    DataField = colName,
                    TypeName = colType.ToString()
                });
            }

            var xml = InternalReportCreation("", "",
                "", description, author, pageHeight, pageWidth, width, topMargin, leftMargin,
                rightMargin, bottomMargin, pageHeaderText, headerTableCells, bodyTableCells, fields, name);

            var rdlp = new RDLParser(xml);

            var fyiReport = await rdlp.Parse();
            ValidateReport(fyiReport);
            await fyiReport.DataSets["Data"].SetData(data);
            return fyiReport;
        }

        
        
        private string GetDataProviderString(DataProviders dataProvider)
        {
            return dataProvider switch
            {
                DataProviders.SqlServer_MicrosoftDataSqlClient => "Microsoft.Data.SqlClient",
                DataProviders.Oracle => "Oracle",
#if WINDOWS
                DataProviders.OleDb => "OLEDB",
#endif
                DataProviders.Odbc => "ODBC",
                DataProviders.Xml => "XML",
                DataProviders.Text => "Text",
                DataProviders.MySql => "MySQL.NET",
                DataProviders.PostgreSQL => "PostgreSQL",
                DataProviders.SqlServer_SystemData => "SQL",
                DataProviders.Firebird => "Firebird.NET 2.0",
                DataProviders.SQLite_MicrosoftData => "Microsoft.Data.Sqlite",
                DataProviders.SQLite_SystemData => "SQLite",
                DataProviders.Json => "Json",
                DataProviders.FileDirectory => "FileDirectory",
                DataProviders.PostgreSQL_Devart => "PostgreSQL_Devart",
                _ => throw new ArgumentOutOfRangeException(nameof(dataProvider), dataProvider, null)
            };
        }
        
        public async Task<Rdl.Report> GenerateRdl(DataProviders dataProvider,
            string connectionString,
            string commandText,
            CommandType commandType = CommandType.Text,
            string description = "",
            string author = "",
            string pageHeight = "11in",
            string pageWidth = "8.5in",
            string width = "7.5in",
            string topMargin = ".25in",
            string leftMargin = ".25in",
            string rightMargin = ".25in",
            string bottomMargin = ".25in",
            string pageHeaderText = "",
            string name = "")
        {
            string providerString = GetDataProviderString(dataProvider);
            return await GenerateRdl(providerString, 
                connectionString, 
                commandText, 
                commandType, 
                description, 
                author, 
                pageHeight, 
                pageWidth, 
                width,
                topMargin,
                leftMargin,
                rightMargin,
                bottomMargin,
                pageHeaderText,
                name);
        }
        
        public async Task<Rdl.Report> GenerateRdl(string dataProvider,
            string connectionString,
            string commandText,
            CommandType commandType = CommandType.Text,
            string description = "",
            string author = "",
            string pageHeight = "11in",
            string pageWidth = "8.5in",
            string width = "7.5in",
            string topMargin = ".25in",
            string leftMargin = ".25in",
            string rightMargin = ".25in",
            string bottomMargin = ".25in",
            string pageHeaderText = "",
            string name = "")
        {

            var headerTableCells = new List<TableCell>();
            var bodyTableCells = new List<TableCell>();
            var fields = new List<Field>();

            using (var cn = RdlEngineConfig.GetConnection(dataProvider, connectionString))
            {
                cn.ConnectionString = connectionString;
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = commandText;
                    cmd.CommandType = commandType;
                    using (var dr = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            string colName = dr.GetName(i);
                            TypeCode colType = Type.GetTypeCode(dr.GetFieldType(i));
                            headerTableCells.Add(new TableCell
                            {
                                ReportItems = new TableCellReportItems()
                                {
                                    ReportItem = new Text
                                    {
                                        Name = $"TextboxH{colName}",
                                        Value = new Value { Text = colName },
                                        Style = new Style { TextAlign = "Center", FontWeight = "Bold" }
                                    }
                                }
                            });

                            bodyTableCells.Add(new TableCell
                            {
                                ReportItems = new TableCellReportItems()
                                {
                                    ReportItem = new Text
                                    {
                                        Name = $"TextBoxB{colName}",
                                        Value = new Value { Text = $"=Fields!{colName}.Value" },
                                        CanGrow = true
                                    }
                                }
                            });

                            fields.Add(new Field
                            {
                                Name = colName,
                                DataField = colName,
                                TypeName = colType.ToString()
                            });

                        }
                    }

                }
            }

            var xml = InternalReportCreation(dataProvider, connectionString,
                commandText, description, author, pageHeight, pageWidth, width, topMargin, leftMargin,
                rightMargin, bottomMargin, pageHeaderText, headerTableCells, bodyTableCells, fields, name);

            var rdlp = new RDLParser(xml);
            var fyiReport = await rdlp.Parse();
            ValidateReport(fyiReport);
            return fyiReport;
        }

        private static string InternalReportCreation(string dataProvider, string connectionString,
            string commandText, string description, string author, string pageHeight, string pageWidth,
            string width, string topMargin, string leftMargin, string rightMargin, string bottomMargin,
            string pageHeaderText, List<TableCell> headerTableCells, List<TableCell> bodyTableCells, List<Field> fields,
            string name)
        {
            // Create a new instance of the Report class
            var report = new Report
            {
                Description = description,
                Author = author,
                Name=name,
                PageHeight = pageHeight,
                PageWidth = pageWidth,
                Width = width,
                TopMargin = topMargin,
                LeftMargin = leftMargin,
                RightMargin = rightMargin,
                BottomMargin = bottomMargin,
                DataSources = new DataSources
                {
                    DataSource = new DataSource
                    {
                        Name = "DS1",
                        ConnectionProperties = new ConnectionProperties
                        {
                            DataProvider = dataProvider,
                            ConnectString = connectionString
                        }
                    }
                },
                DataSets = new DataSets
                {
                    DataSet = new DataSet
                    {
                        Name = "Data",
                        Query = new Query
                        {
                            DataSourceName = "DS1",
                            CommandText = commandText
                        },
                        Fields = new Fields
                        {
                            Field = fields
                        }
                    }
                },
                PageHeader = new PageHeader
                {
                    Height = ".5in",
                    ReportItems = new ReportItemsHeader
                    {
                        Textbox = new Text
                        {
                            Name = "Textbox1",
                            Top = ".1in",
                            Left = ".1in",
                            Width = "6in",
                            Height = ".25in",
                            Value = new Value { Text = pageHeaderText },
                            Style = new Style { FontSize = "15pt", FontWeight = "Bold" }
                        }
                    },
                    PrintOnFirstPage = "true",
                    PrintOnLastPage = "true"
                },
                Body = new Body
                {
                    ReportItems = new ReportItemsBody
                    {
                        Table = new Table
                        {
                            TableName = "Table1",
                            DataSetName = "Data",
                            NoRows = "Query returned no rows!",
                            TableColumns = new TableColumns
                            {
                                TableColumn = fields.Select(f => new TableColumn
                                {
                                    Width = "1in" // You can adjust the width as needed
                                }).ToList()
                            },
                            Header = new Header
                            {
                                TableRows = new TableRows
                                {
                                    TableRow = new TableRow
                                    {
                                        Height = "12pt",
                                        TableCells = new TableCells()
                                        {
                                            TableCell = headerTableCells
                                        }
                                    }
                                },
                                RepeatOnNewPage = "true"
                            },
                            Details = new Details
                            {
                                TableRows = new TableRows
                                {
                                    TableRow = new TableRow
                                    {
                                        Height = "12pt",
                                        TableCells = new TableCells()
                                        {
                                            TableCell = bodyTableCells
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Height = "36pt"
                },
                PageFooter = new PageFooter
                {
                    Height = "14pt",
                    ReportItems = new ReportItemsFooter
                    {
                        Textbox = new Text
                        {
                            Name = "Textbox5",
                            Top = "1pt",
                            Left = "10pt",
                            Height = "12pt",
                            Width = "3in",
                            Value = new Value { Text = "=Globals!PageNumber + ' of ' + Globals!TotalPages" },
                            Style = new Style { FontSize = "10pt", FontWeight = "Normal" }
                        }
                    },
                    PrintOnFirstPage = "true",
                    PrintOnLastPage = "true"
                }
            };

            var serializer = new XmlSerializer(typeof(Report));
            string xml;
            using (var writer = new Utf8StringWriter())
            {
                serializer.Serialize(writer, report);
                xml = writer.ToString();
            }

            return xml;
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

}
