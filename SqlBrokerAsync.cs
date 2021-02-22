using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using BetterTechnologies.SimpleSql.Extensions;
using BetterTechnologies.SimpleSql.SqlDataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTechnologies.SimpleSql
{
    public partial class SqlBroker
    {
        async Task<SqlCommand> GetLoadCommandAsync(DataObject o, QueryFilters filter = null)
        {
            var sqlCmd = new SqlCommand() { Connection = await GetConnectionAsync(true) };

            if (o.ProcName != null)
            {
                if (filter != null)
                    throw new Exception("Filters not supported with stored procedure.");

                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCmd.CommandText = o.ProcName;
            }
            else
            {
                var sQry = new SelectQuery(o.TableName);
                var sqlParams = new SqlParameters(sqlCmd.Parameters);

                if (filter != null)
                {
                    var obj = Activator.CreateInstance(o.Type);

                    foreach (var f in filter)
                    { // Use the alias if there is one, or if not use the property name
                        if (o.Properties[f.Property] != null)
                            f.Alias = o.Properties[f.Property].Alias;
                        else
                            f.Alias = f.Property;
                    }

                    sQry.WhereValuePairs = filter.Select(p => new SqlValuePair(p.Alias, sqlParams.Add(p.Value))).ToList();
                }
                sqlCmd.CommandText = sQry.ToString();
            }

            return sqlCmd;
        }

        public async Task<List<T>> LoadAsync<T>(object parameters, string storedProc = null)
        {
            var parms = parameters.ConvertToDictionary();
            var sqlParameters = parms.Select(p => new SqlParameter(p.Key, p.Value ?? DBNull.Value));

            var o = new DataObject(typeof(T), ImplicitMapping);
            var sqlCmd = new SqlCommand() { Connection = await GetConnectionAsync(true) };

            sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCmd.CommandText = storedProc ?? o.ProcName;

            foreach (var p in sqlParameters)
                sqlCmd.Parameters.AddWithValue(p.ParameterName, p.Value);

            var results = new List<T>();

            using (var dr = await sqlCmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection))
                while (dr.Read())
                    results.Add((T)FillObject(dr, o.Type, o.Properties));

            return results;
        }

        public async Task<List<T>> LoadAsync<T>()
        {
            var o = new DataObject(typeof(T));
            var sqlCmd = await GetLoadCommandAsync(o);
            var results = new List<T>();

            using (var dr = await sqlCmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection))
                while (dr.Read())
                    results.Add((T)FillObject(dr, o.Type, o.Properties));

            return results;
        }

        public async Task<T> GetJsonResultAsync<T>(string command, object parameters)
        {
            using (SqlDataReader reader = await ExecuteReaderAsync(command, parameters))
            {
                StringBuilder sb = new StringBuilder();
                while (reader.Read())
                    sb.Append(reader[0]);

                string s = sb.ToString();
                var result = JsonConvert.DeserializeObject<T>(s);
                return result;
            }
        }

        async Task<SqlDataReader> ExecuteReaderAsync(string command, object parameters, bool isProc = true)
        {
            SqlDataReader reader = null;
            await UsingSqlCommandAsync(command, parameters, isProc, async cmd =>
            {
                cmd.Connection.Open();
                reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            });
            return reader;
        }

        async Task<object> ExecuteSqlScalarAsync(SqlCommand sqlCmd)
        {
            try
            {
                sqlCmd.Connection.Open();
                var ret = await sqlCmd.ExecuteScalarAsync();
                return ret;
            }
            finally { sqlCmd.Connection.Close(); }
        }

        public async Task<T> ExecuteScalarAsync<T>(string command, object parameters, bool isProc = true)
        {
            T retValue = default(T);
            await UsingSqlCommandAsync(command, parameters, isProc, async cmd => { retValue = (T)(await ExecuteSqlScalarAsync(cmd)); });
            return retValue;
        }

        public async Task<T> LoadOne<T>(string storedProc, object parameters)
        {
            var result = await LoadAsync<T>(parameters, storedProc);
            return result.First();
        }

        public async Task<bool> IsTrue(string storedProc, object parameters)
        {
            return await ExecuteScalarAsync<bool>(storedProc, parameters);
        }
    }
}
