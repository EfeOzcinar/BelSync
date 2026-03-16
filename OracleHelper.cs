using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Newtonsoft.Json.Linq;

namespace BelSync
{
    public static class OracleHelper
    {
        private static AppSettings _settings;

        public static void Configure(AppSettings settings) => _settings = settings;

        private static readonly Dictionary<string, string> SchemaPasswords =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string BuildConnectionString(string schema)
        {
            string user = schema;
            string pass = SchemaPasswords.TryGetValue(schema, out string mapped)
                          ? mapped : schema.ToLower();
            string host = _settings?.Host ?? "10.180.27.52";
            string port = _settings?.Port ?? "1521";
            string svc = _settings?.Service ?? "orcl";

            return $"User Id={user};Password={pass};Connection Timeout=15;" +
                   $"Min Pool Size=0;Max Pool Size=5;Connection Lifetime=60;" +
                   $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))" +
                   $"(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={svc})));";
        }

        private static string AdminConnectionString()
        {
            string admin = _settings?.AdminUser ?? "PRM";
            return BuildConnectionString(admin);
        }

        // ── Get schemas ────────────────────────────────────────────────
        public static List<string> GetSchemas()
        {
            var list = new List<string>();
            using (var conn = new OracleConnection(AdminConnectionString()))
            {
                conn.Open();
                string sql;
                try
                {
                    new OracleCommand("SELECT COUNT(*) FROM DBA_USERS", conn).ExecuteScalar();
                    sql = @"SELECT USERNAME FROM DBA_USERS
                            WHERE ACCOUNT_STATUS='OPEN'
                            AND USERNAME NOT IN (
                                'SYS','SYSTEM','OUTLN','DBSNMP','APPQOSSYS','DBSFWUSER',
                                'GGSYS','ANONYMOUS','CTXSYS','DVSYS','DVF','GSMADMIN_INTERNAL',
                                'MDSYS','OLAPSYS','XDB','WMSYS','ORDDATA','LBACSYS','ORDPLUGINS',
                                'ORDSYS','SI_INFORMTN_SCHEMA','OJVMSYS','AUDSYS','ORACLE_OCM',
                                'REMOTE_SCHEDULER_AGENT','XS$NULL','GSMCATUSER','MDDATA',
                                'SYSBACKUP','SYSDG','SYSKM','SYSRAC','DIP','APEX_PUBLIC_USER'
                            ) ORDER BY USERNAME";
                }
                catch
                {
                    sql = @"SELECT USERNAME FROM ALL_USERS
                            WHERE USERNAME NOT IN (
                                'SYS','SYSTEM','OUTLN','DBSNMP','APPQOSSYS','DBSFWUSER',
                                'GGSYS','ANONYMOUS','CTXSYS','DVSYS','DVF','GSMADMIN_INTERNAL',
                                'MDSYS','OLAPSYS','XDB','WMSYS','ORDDATA','LBACSYS','ORDPLUGINS',
                                'ORDSYS','SI_INFORMTN_SCHEMA','OJVMSYS','AUDSYS','ORACLE_OCM',
                                'REMOTE_SCHEDULER_AGENT','XS$NULL','GSMCATUSER','MDDATA',
                                'SYSBACKUP','SYSDG','SYSKM','SYSRAC','DIP','APEX_PUBLIC_USER'
                            ) ORDER BY USERNAME";
                }
                using (var cmd = new OracleCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(rdr.GetString(0));
            }
            return list;
        }

        // ── Get tables ─────────────────────────────────────────────────
        public static List<string> GetTables(string schema)
        {
            var list = new List<string>();
            using (var conn = new OracleConnection(BuildConnectionString(schema)))
            {
                conn.Open();
                using (var cmd = new OracleCommand(
                    "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME", conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(rdr.GetString(0));
            }
            return list;
        }

        // ── Get columns ────────────────────────────────────────────────
        public static List<ColumnInfo> GetColumns(string schema, string table)
        {
            var list = new List<ColumnInfo>();
            using (var conn = new OracleConnection(BuildConnectionString(schema)))
            {
                conn.Open();
                using (var cmd = new OracleCommand(
                    @"SELECT COLUMN_NAME, DATA_TYPE, NULLABLE
                      FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:t ORDER BY COLUMN_ID", conn))
                {
                    cmd.Parameters.Add("t", OracleDbType.Varchar2).Value = table;
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(new ColumnInfo
                            {
                                Name = rdr.GetString(0),
                                DataType = rdr.GetString(1),
                                NotNull = rdr.GetString(2) == "N"
                            });
                }
            }
            return list;
        }

        // ── Flatten JSON ───────────────────────────────────────────────
        public static List<(string Key, string Value)> FlattenJson(JToken token, string prefix = "")
        {
            var result = new List<(string, string)>();
            if (token is JObject obj)
                foreach (var prop in obj.Properties())
                {
                    string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    result.AddRange(FlattenJson(prop.Value, key));
                }
            else if (token is JArray arr)
                for (int i = 0; i < arr.Count; i++)
                    result.AddRange(FlattenJson(arr[i], $"{prefix}[{i}]"));
            else
                result.Add((prefix, token.Type == JTokenType.Null ? "" : token.ToString()));
            return result;
        }

        // ── Sync: check by key, insert key+value if key doesn't exist ───
        public static SyncResult Sync(string schema, string table, string keyCol,
                                      List<(string Key, string Value)> pairs,
                                      bool preview = false)
        {
            var result = new SyncResult();
            string fullTbl = $"{schema}.{table}";

            using (var conn = new OracleConnection(BuildConnectionString(schema)))
            {
                conn.Open();
                bool hasOid = HasOidColumn(conn, table);
                var cols = GetColumnsInternal(conn, table);
                long maxOid = hasOid ? GetMaxOid(conn, fullTbl) : 0;
                long nextOid = maxOid + 1;

                OracleTransaction tran = preview ? null : conn.BeginTransaction();
                try
                {
                    foreach (var (key, value) in pairs)
                    {
                        bool exists = KeyExists(conn, fullTbl, keyCol, key);
                        if (exists)
                        {
                            result.Skipped++;
                            result.Rows.Add(new SyncRow { Key = key, Value = value, Status = RowStatus.Skipped });
                        }
                        else
                        {
                            if (!preview)
                            {
                                InsertRecord(conn, fullTbl, new List<(string, string)> { (key, value) }, cols, nextOid, hasOid);
                                if (result.RollbackOidFrom == 0) result.RollbackOidFrom = maxOid;
                                result.RollbackOidTo = nextOid;
                                nextOid++;
                            }
                            result.Inserted++;
                            result.Rows.Add(new SyncRow { Key = key, Value = value, Status = RowStatus.Inserted });
                        }
                    }
                    tran?.Commit();
                }
                catch { tran?.Rollback(); throw; }
                finally { tran?.Dispose(); }
            }
            return result;
        }

        // ── Rollback ───────────────────────────────────────────────────
        public static int Rollback(string schema, string table, long oidFrom, long oidTo)
        {
            using (var conn = new OracleConnection(BuildConnectionString(schema)))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                    try
                    {
                        using (var cmd = new OracleCommand(
                            $"DELETE FROM {schema}.{table} WHERE OID > :f AND OID <= :t", conn))
                        {
                            cmd.Parameters.Add("f", OracleDbType.Int64).Value = oidFrom;
                            cmd.Parameters.Add("t", OracleDbType.Int64).Value = oidTo;
                            int deleted = cmd.ExecuteNonQuery();
                            tran.Commit();
                            return deleted;
                        }
                    }
                    catch { tran.Rollback(); throw; }
            }
        }

        // ── Private helpers ────────────────────────────────────────────
        private static bool KeyExists(OracleConnection conn, string fullTbl, string keyCol, string keyVal)
        {
            using (var cmd = new OracleCommand($"SELECT COUNT(*) FROM {fullTbl} WHERE {keyCol}=:kv", conn))
            {
                cmd.Parameters.Add("kv", OracleDbType.Varchar2).Value = keyVal;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }



        private static long GetMaxOid(OracleConnection conn, string fullTbl)
        {
            using (var cmd = new OracleCommand($"SELECT NVL(MAX(OID),0) FROM {fullTbl}", conn))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private static bool HasOidColumn(OracleConnection conn, string table)
        {
            using (var cmd = new OracleCommand(
                "SELECT COUNT(*) FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:t AND COLUMN_NAME='OID'", conn))
            {
                cmd.Parameters.Add("t", OracleDbType.Varchar2).Value = table;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static List<ColumnInfo> GetColumnsInternal(OracleConnection conn, string table)
        {
            var list = new List<ColumnInfo>();
            using (var cmd = new OracleCommand(
                "SELECT COLUMN_NAME,DATA_TYPE,NULLABLE FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:t ORDER BY COLUMN_ID", conn))
            {
                cmd.Parameters.Add("t", OracleDbType.Varchar2).Value = table;
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new ColumnInfo
                        {
                            Name = rdr.GetString(0),
                            DataType = rdr.GetString(1),
                            NotNull = rdr.GetString(2) == "N"
                        });
            }
            return list;
        }

        private static void InsertRecord(OracleConnection conn, string fullTbl,
            List<(string Key, string Value)> pairs, List<ColumnInfo> columns,
            long oid, bool hasOid)
        {
            var now = DateTime.Now;
            long lastUpd = long.Parse(now.ToString("yyyyMMddHHmmss"));
            int procDate = int.Parse(now.ToString("yyyyMMdd"));
            int procTime = int.Parse(now.ToString("HHmmss"));

            var jsonLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in pairs) jsonLookup[k] = v;

            var colNames = new List<string>();
            var paramVals = new Dictionary<string, object>();

            if (hasOid) { colNames.Add("OID"); paramVals["p_OID"] = oid; }

            foreach (var col in columns)
            {
                if (col.Name == "OID") continue;
                string pName = "p_" + col.Name;
                if (jsonLookup.TryGetValue(col.Name, out string jv))
                { colNames.Add(col.Name); paramVals[pName] = ConvertValue(jv, col.DataType); }
                else if (col.NotNull)
                { colNames.Add(col.Name); paramVals[pName] = GetDefault(col.Name, col.DataType, oid, lastUpd, procDate, procTime); }
            }

            string sql = $"INSERT INTO {fullTbl} ({string.Join(",", colNames)}) " +
                         $"VALUES ({string.Join(",", colNames.ConvertAll(c => ":p_" + c))})";

            using (var cmd = new OracleCommand(sql, conn))
            {
                foreach (var kv in paramVals)
                    cmd.Parameters.Add(kv.Key, kv.Value ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static object ConvertValue(string value, string dataType)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;
            if (dataType.StartsWith("NUMBER") || dataType == "INTEGER")
                if (decimal.TryParse(value, out decimal d)) return d;
            return value;
        }

        private static object GetDefault(string col, string dt, long oid,
                                         long lastUpd, int procDate, int procTime)
        {
            string c = col.ToUpper();
            if (c == "STATUS") return 1;
            if (c == "LASTUPDATED") return lastUpd;
            if (c == "OPERATIONREFNO") return oid;
            if (c == "PROCESS_DATE") return procDate;
            if (c == "PROCESS_TIME") return procTime;
            if (c == "IS_UPDATABLE") return "Y";
            if (c == "DATA_TYPE") return "STRING";
            if (c.Contains("CODE")) return 1;
            if (c.Contains("TYPE")) return 1;
            if (c.Contains("FLAG")) return 0;
            if (dt.StartsWith("NUMBER") || dt == "INTEGER") return 0;
            if (dt.StartsWith("VARCHAR") || dt == "CHAR") return "-";
            if (dt == "DATE") return DateTime.Now;
            return DBNull.Value;
        }
    }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool NotNull { get; set; }
    }

    public enum RowStatus { Inserted, Skipped }

    public class SyncRow
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public RowStatus Status { get; set; }
    }

    public class SyncResult
    {
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public long RollbackOidFrom { get; set; }
        public long RollbackOidTo { get; set; }
        public List<SyncRow> Rows { get; set; } = new List<SyncRow>();
        public int Total => Inserted + Skipped;
    }
}