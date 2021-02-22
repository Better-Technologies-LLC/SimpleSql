using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Data;
using Newtonsoft.Json;
using System.Text;
using BetterTechnologies.SimpleSql.Extensions;

namespace BetterTechnologies.SimpleSql
{
    public partial class SqlBroker : SqlBrokerBase
    {
        public SqlBroker(SqlBrokerOptions options) : base(options) { }

        // Send parameters in as anon object. For example, var parameters = new { p1 = 123, p2 = "abc" }
        public List<T> Load<T>(object parameters, string storedProc = null)
        {
            var parms = parameters.ConvertToDictionary();
            var sqlParameters = parms.Select(p => new SqlParameter(p.Key, p.Value ?? DBNull.Value));

            var o = new DataObject(typeof(T));
            var sqlCmd = new SqlCommand() { Connection = GetConnection(true) };

            sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCmd.CommandText = storedProc ?? o.ProcName;

            foreach (var p in sqlParameters)
                sqlCmd.Parameters.AddWithValue(p.ParameterName, p.Value);

            var results = new List<T>();

            using (var dr = sqlCmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                while (dr.Read())
                    results.Add((T)FillObject(dr, o.Type, o.Properties));

            return results;
        }

        public T GetJsonResult<T>(string command, object parameters)
        {
            using (SqlDataReader reader = ExecuteReader(command, parameters))
            {
                StringBuilder sb = new StringBuilder();
                while (reader.Read())
                    sb.Append(reader[0]);

                string s = sb.ToString();
                var result = JsonConvert.DeserializeObject<T>(s);
                return result;
            }
        }

        public string GetResultsAsJson(string command, object parameters, bool isProc = true)
        {
            using (SqlDataReader reader = ExecuteReader(command, parameters))
            {
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return JsonConvert.SerializeObject(dataTable);
            }
        }

        public SqlCommandResponse ExecuteNonQuery(string command, object parameters, bool isProc = true)
        {
            SqlCommandResponse r = null;
            UsingSqlCommand(command, parameters, isProc, cmd => r = ExecuteSqlCommand(cmd));
            return r;
        }

        public T ExecuteScalar<T>(string command, object parameters, bool isProc = true)
        {
            T retValue = default(T);
            UsingSqlCommand(command, parameters, isProc, cmd => { retValue = (T)ExecuteSqlScalar(cmd); });
            return retValue;
        }

        SqlDataReader ExecuteReader(string command, object parameters, bool isProc = true)
        {
            SqlDataReader reader = null;
            UsingSqlCommand(command, parameters, isProc, cmd =>
            {
                cmd.Connection.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            });
            return reader;
        }


    }
}