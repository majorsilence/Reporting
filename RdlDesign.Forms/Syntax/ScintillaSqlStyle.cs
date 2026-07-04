using Majorsilence.Reporting.RdlDesign.Syntax;
using System.Drawing;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    public class ScintillaSqlStyle
    {
        private Scintilla scintilla;
        public ScintillaSqlStyle(Scintilla scintilla)
        {
            this.scintilla = scintilla;
            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Courier New";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();

            // Set the SQL Lexer
            scintilla.Lexer = Lexer.Sql;

            // Show line numbers
            scintilla.Margins[0].Width = 20;

            // Set the Styles
            scintilla.Styles[Style.LineNumber].ForeColor = Color.FromArgb(255, 128, 128, 128);  //Dark Gray
            scintilla.Styles[Style.LineNumber].BackColor = Color.FromArgb(255, 228, 228, 228);  //Light Gray
            scintilla.Styles[Style.Sql.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Sql.CommentLine].ForeColor = Color.Green;
            scintilla.Styles[Style.Sql.CommentLineDoc].ForeColor = Color.Green;
            scintilla.Styles[Style.Sql.Number].ForeColor = Color.Maroon;
            scintilla.Styles[Style.Sql.Word].ForeColor = Color.Blue;
            scintilla.Styles[Style.Sql.Word2].ForeColor = Color.Fuchsia;
            scintilla.Styles[Style.Sql.User1].ForeColor = Color.Gray;
            scintilla.Styles[Style.Sql.User2].ForeColor = Color.FromArgb(255, 00, 128, 192);    //Medium Blue-Green
            scintilla.Styles[Style.Sql.String].ForeColor = Color.Red;
            scintilla.Styles[Style.Sql.Character].ForeColor = Color.Red;
            scintilla.Styles[Style.Sql.Operator].ForeColor = Color.Black;

            //Brace Matching
            scintilla.IndentationGuides = IndentView.LookBoth;
            scintilla.Styles[Style.BraceLight].BackColor = Color.LightGray;
            scintilla.Styles[Style.BraceLight].ForeColor = Color.BlueViolet;
            scintilla.Styles[Style.BraceBad].ForeColor = Color.Red;
            scintilla.UpdateUI += scintilla_UpdateUI;

            // Set keyword lists
            // This keywords base on MSSQL recept with adding MariaDB\MySQL function lists.
            // For another SQL servers maybe need create separate style
            // Word = 0 (commands)
            scintilla.SetKeywords(0, @"add alter as authorization backup begin bigint binary bit break browse bulk by cascade case catch check checkpoint close clustered column commit compute constraint containstable continue create current cursor cursor database date datetime datetime2 datetimeoffset dbcc deallocate decimal declare default delete deny desc disk distinct distributed double drop dump else end errlvl escape except exec execute exit external fetch file fillfactor float for foreign freetext freetexttable from full function goto grant group having hierarchyid holdlock identity identity_insert identitycol if image index insert int intersect into key kill lineno load merge money national nchar nocheck nocount nolock nonclustered ntext numeric nvarchar of off offsets on open opendatasource openquery openrowset openxml option order over percent plan precision primary print proc procedure public raiserror read readtext real reconfigure references replication restore restrict return revert revoke rollback rowcount rowguidcol rule save schema securityaudit select set setuser shutdown smalldatetime smallint smallmoney sql_variant statistics table table tablesample text textsize then time timestamp tinyint to top tran transaction trigger truncate try union unique uniqueidentifier update updatetext use user values varbinary varchar varying view waitfor when where while with writetext xml go ");
            // Word2 = 1 (function)
            scintilla.SetKeywords(1, "abs acos adddate addtime aes_decrypt aes_encrypt area asbinary ascii asin astext aswkb aswkt atan atan2 avg " +
                "benchmark bin binlog_gtid_pos bit_and bit_count bit_length bit_or bit_xor boundary buffer " +
                "cast ceil ceiling centroid char charindex character_length char_length charset chr coalesce coercibility collate column_add column_check column_create column_delete column_exists column_get column_json column_list compress concat concat_ws connection_id contains convert conv convert_tz convexhull cos cot count crc32 crosses cume_dist curdate current_date current_role current_time current_timestamp current_user curtime " +
                "database datediff date_add date_format date_sub day dayname dayofmonth dayofweek dayofyear decode decode_histogram default degrees dense_rank des_decrypt des_encrypt dimension disjoint " +
                "elt encode encrypt endpoint envelope equals exp export_set extract extractvalue " +
                "field find_in_set floor format found_rows from_base64 from_days from_unixtime " +
                "get_format greatest group_concat " +
                "hex hour " +
                "ifnull interval inet6_aton inet6_ntoa inet_ntoa instr intersects is_free_lock is_ipv4 is_ipv4_compat is_ipv4_mapped is_ipv6 isnull " +
                "last_day last_insert_id last_value lastval lcase least length linestring load_file localtime localtimestamp locate log log10 log2 lower lpad ltrim " +
                "make_set makedate maketime max md5 median microsecond mid min minute month monthname multilinestring " +
                "name_const nullif nextval now ntile " +
                "object_id oct octet_length old_password ord overlaps " +
                "password percent_rank percent_rank percentile_cont percentile_disc period_add period_diff pi point polygon position pow power " +
                "quarter quote " +
                "radians rand rank regexp regexp_instr regexp_replace regexp_substr release_lock repeat replace reverse right rlike rpad round row_count row_number rtrim " +
                "schema second sec_to_time setval session_user sha sha1 sha2 sign sin soundex space sqrt startpoint str_to_date strcmp subdate substr substring substring_index subtime sum sysdate system_user " +
                "tan time timediff timestamp timestampadd timestampdiff time_format time_to_sec to_base64 to_days to_seconds touches trim truncate tsequal " +
                "ucase unhex uncompress uncompressed_length unix_timestamp updatexml upper user utc_date utc_time utc_timestamp uuid uuid_short " +
                "version " +
                "week weekday weekdaynonamerican weekofyear weight_string within " +
                "year yearweek");
            // User1 = 4 (expresions)
            scintilla.SetKeywords(4, @"all and any between cross div exists in inner is join left like mod not null or outer pivot right separator some unpivot ( ) * ");
            // User2 = 5
            scintilla.SetKeywords(5, @"sys objects sysobjects ");
        }
        
        private static bool IsBrace(int c)
        {
            switch (c)
            {
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '<':
                case '>':
                    return true;
            }

            return false;
        }

        int lastCaretPos = 0;

        private void scintilla_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            // Has the caret changed position?
            var caretPos = scintilla.CurrentPosition;
            if (lastCaretPos != caretPos)
            {
                lastCaretPos = caretPos;
                var bracePos1 = -1;
                var bracePos2 = -1;

                // Is there a brace to the left or right?
                if (caretPos > 0 && IsBrace(scintilla.GetCharAt(caretPos - 1)))
                    bracePos1 = (caretPos - 1);
                else if (IsBrace(scintilla.GetCharAt(caretPos)))
                    bracePos1 = caretPos;

                if (bracePos1 >= 0)
                {
                    // Find the matching brace
                    bracePos2 = scintilla.BraceMatch(bracePos1);
                    if (bracePos2 == Scintilla.InvalidPosition)
                    {
                        scintilla.BraceBadLight(bracePos1);
                        scintilla.HighlightGuide = 0;
                    }
                    else
                    {
                        scintilla.BraceHighlight(bracePos1, bracePos2);
                        scintilla.HighlightGuide = scintilla.GetColumn(bracePos1);
                    }
                }
                else
                {
                    // Turn off brace matching
                    scintilla.BraceHighlight(Scintilla.InvalidPosition, Scintilla.InvalidPosition);
                    scintilla.HighlightGuide = 0;
                }
            }
        }
    }
}
