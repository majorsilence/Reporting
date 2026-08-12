using System;
using System.Data;
using System.IO;
using Majorsilence.Reporting.Data;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The file/web based providers in Majorsilence.Reporting.DataProviders used to accept only
    /// the exact CommandBehavior values SingleResult and SchemaOnly, throwing ArgumentException
    /// on anything else - including CommandBehavior.Default and including ordinary flag
    /// combinations such as SingleResult | SchemaOnly. Since CommandBehavior is a [Flags] enum,
    /// they now test flags and accept anything they can honour or safely ignore.
    /// </summary>
    [TestFixture]
    public class DataProviderCommandBehaviorTests
    {
        private string _jsonFile;

        [SetUp]
        public void SetUp()
        {
            _jsonFile = Path.Combine(Path.GetTempPath(), $"rdl-behavior-{Guid.NewGuid():N}.json");
            File.WriteAllText(_jsonFile, """
                [{"Name":"Alice","Age":30},{"Name":"Bob","Age":25}]
                """);
        }

        [TearDown]
        public void TearDown()
        {
            try { File.Delete(_jsonFile); }
            catch (IOException) { }
        }

        private IDbCommand CreateCommand(out IDbConnection connection)
        {
            connection = new JsonConnection($"file={_jsonFile}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "root";
            return cmd;
        }

        private static int CountRows(IDataReader dr)
        {
            int n = 0;
            while (dr.Read()) n++;
            return n;
        }

        [Test]
        public void Default_IsAcceptedAndReturnsRows()
        {
            using var cmd = CreateCommand(out var cn);
            using (cn)
            {
                using IDataReader dr = cmd.ExecuteReader(CommandBehavior.Default);
                Assert.That(CountRows(dr), Is.EqualTo(2));
            }
        }

        [Test]
        public void SingleResult_StillAcceptedAndReturnsRows()
        {
            using var cmd = CreateCommand(out var cn);
            using (cn)
            {
                using IDataReader dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
                Assert.That(CountRows(dr), Is.EqualTo(2));
            }
        }

        [Test]
        public void SchemaOnly_StillAcceptedAndExposesColumns()
        {
            using var cmd = CreateCommand(out var cn);
            using (cn)
            {
                using IDataReader dr = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
                Assert.That(dr.FieldCount, Is.GreaterThan(0), "Column metadata should still be available");
            }
        }

        [Test]
        public void FlagCombinations_AreAccepted()
        {
            // Each of these was rejected by the old whole-value comparison.
            CommandBehavior[] combinations =
            {
                CommandBehavior.SingleResult | CommandBehavior.SingleRow,
                CommandBehavior.SingleResult | CommandBehavior.SequentialAccess,
                CommandBehavior.SingleResult | CommandBehavior.SchemaOnly,
                CommandBehavior.KeyInfo,
            };

            foreach (CommandBehavior behavior in combinations)
            {
                using var cmd = CreateCommand(out var cn);
                using (cn)
                {
                    Assert.DoesNotThrow(() => cmd.ExecuteReader(behavior).Dispose(),
                        $"CommandBehavior.{behavior} should be accepted");
                }
            }
        }

        [Test]
        public void CloseConnection_IsRejectedWithAClearMessage()
        {
            // The one flag that is still refused: these readers do not own the connection they
            // are handed, so silently ignoring CloseConnection would leak it.
            using var cmd = CreateCommand(out var cn);
            using (cn)
            {
                var ex = Assert.Throws<ArgumentException>(
                    () => cmd.ExecuteReader(CommandBehavior.CloseConnection).Dispose());
                Assert.That(ex.Message, Does.Contain("CloseConnection"));
            }
        }
    }
}
