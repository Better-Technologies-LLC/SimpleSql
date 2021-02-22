using Microsoft.Data.SqlClient;
using BetterTechnologies.SimpleSql.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterTechnologies.SimpleSql
{
    public class SqlBrokerBase
    {
        protected string ConnectionString { get; set; }
        protected bool ImplicitMapping { get; set; }

        public SqlBrokerBase(SqlBrokerOptions options)
        {
            ConnectionString = options.ConnectionString;
            ImplicitMapping = options.ImplicitMapping;
        }

        public int? CommandTimeout { get; set; }

        public SqlConnection GetConnection(bool open = false)
        {
            try
            {
                SqlConnection c = new SqlConnection(ConnectionString);
                if (open)
                    c.Open();
                return c;
            }
            catch (Exception ex) { throw new Exception("GetConnection: " + ex.Message, ex); }
        }

        public async Task<SqlConnection> GetConnectionAsync(bool open = false)
        {
            try
            {
                SqlConnection c = new SqlConnection(ConnectionString);
                if (open)
                    await c.OpenAsync();
                return c;
            }
            catch (Exception ex) { throw new Exception("GetConnection: " + ex.Message, ex); }
        }

        protected object FillObject(SqlDataReader dr, Type t, DataObjectProperties props)
        {
            // Cache the field indexes because it's faster to reference them by number than by name
            Dictionary<string, int> fieldNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < dr.FieldCount; i++)
                fieldNumbers.Add(dr.GetName(i), i);

            var availableProps = props.Where(o => fieldNumbers.ContainsKey(o.Alias));

            try
            {
                var obj = Activator.CreateInstance(t);

                foreach (var p in availableProps)
                {
                    var i = fieldNumbers[p.Alias];

                    if (dr.IsDBNull(i))
                        continue; // Proactively testing for null makes this marginally faster

                    var value = dr[i];
                    p.SetValue(obj, value);
                }

                return obj;
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("SimpleSql: Error filling DataRow with query data.  Check to make sure the DataRow's field names are spelled correctly and are using correct data types.");
            }
            catch (Exception ex)
            {
                // For invalid cast exceptions, check to see if maybe the db field is a different kind of integer. 
                // I.e. maybe it's a smallint and you're using a regular int in c# instead of a short
                throw new Exception("FillObjects: " + ex.Message, ex);
            }
        }

        protected SqlCommandResponse ExecuteSqlCommand(SqlCommand sqlCmd)
        {
            try
            {
                sqlCmd.Parameters.Add("@retValue", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.ReturnValue;

                if (!_trxStarted) // default
                    sqlCmd.Connection.Open();
                else if (_transaction != null) // trx already initiated
                {
                    // connection is already open so no need to open again
                    sqlCmd.Connection = _transaction.Connection;
                    sqlCmd.Transaction = _transaction;
                }
                else // StartTransaction() called but trx not yet initiated
                {
                    sqlCmd.Connection.Open();
                    _transaction = sqlCmd.Connection.BeginTransaction();
                    sqlCmd.Transaction = _transaction;
                }

                var r = new SqlCommandResponse();
                r.RowsAffected = sqlCmd.ExecuteNonQuery();
                r.ReturnValue = (int)sqlCmd.Parameters["@retValue"].Value;
                return r;
            }
            finally
            {
                if (!_trxStarted)
                    sqlCmd.Connection.Close();
            }
        }

        protected async Task<SqlCommandResponse> ExecuteSqlCommandAsync(SqlCommand sqlCmd)
        {
            try
            {
                sqlCmd.Parameters.Add("@retValue", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.ReturnValue;

                if (!_trxStarted) // default
                    sqlCmd.Connection.Open();
                else if (_transaction != null) // trx already initiated
                {
                    // connection is already open so no need to open again
                    sqlCmd.Connection = _transaction.Connection;
                    sqlCmd.Transaction = _transaction;
                }
                else // StartTransaction() called but trx not yet initiated
                {
                    sqlCmd.Connection.Open();
                    _transaction = sqlCmd.Connection.BeginTransaction();
                    sqlCmd.Transaction = _transaction;
                }

                var r = new SqlCommandResponse();
                r.RowsAffected = await sqlCmd.ExecuteNonQueryAsync();
                r.ReturnValue = (int)sqlCmd.Parameters["@retValue"].Value;
                return r;
            }
            finally
            {
                if (!_trxStarted)
                    sqlCmd.Connection.Close();
            }
        }

        public async Task<SqlCommandResponse> ExecuteNonQueryAsync(string command, object parameters, bool isProc = true)
        {
            SqlCommandResponse r = null;
            await UsingSqlCommandAsync(command, parameters, isProc, async cmd => r = await ExecuteSqlCommandAsync(cmd));
            return r;
        }

        public void CommitTransaction()
        {
            SqlConnection cx = null;

            try
            {
                if (_transaction != null)
                {
                    cx = _transaction.Connection;
                    _transaction.Commit();
                }
            }
            finally
            {
                if (cx != null && cx.State != System.Data.ConnectionState.Closed)
                    cx.Close();

                _trxStarted = false;
                _transaction = null;
            }
        }

        protected object ExecuteSqlScalar(SqlCommand sqlCmd)
        {
            try
            {
                sqlCmd.Connection.Open();
                var ret = sqlCmd.ExecuteScalar();
                return ret;
            }
            finally { sqlCmd.Connection.Close(); }
        }

        protected async Task UsingSqlCommandAsync(string command, object parameters, bool isProc, Func<SqlCommand, Task> action)
        {
            var parms = parameters.ConvertToDictionary();
            var sqlCmd = new SqlCommand(command, await GetConnectionAsync()) { CommandType = isProc ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text };

            if (CommandTimeout != null)
                sqlCmd.CommandTimeout = this.CommandTimeout.Value;

            foreach (var kvp in parms)
                sqlCmd.Parameters.AddWithValue(kvp.Key, kvp.Value);

            await action(sqlCmd);
        }

        protected void UsingSqlCommand(string command, object parameters, bool isProc, Action<SqlCommand> action)
        {
            var parms = parameters.ConvertToDictionary();
            var sqlCmd = new SqlCommand(command, GetConnection()) { CommandType = isProc ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text };

            if (CommandTimeout != null)
                sqlCmd.CommandTimeout = this.CommandTimeout.Value;

            foreach (var kvp in parms)
                sqlCmd.Parameters.AddWithValue(kvp.Key, kvp.Value);

            action(sqlCmd);
        }

        protected SqlTransaction _transaction = null;
        protected bool _trxStarted = false;
        public void StartTransaction()
        {
            _trxStarted = true; ;
        }
    }
}