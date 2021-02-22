using FastMember;
using Microsoft.Data.SqlClient;
using BetterTechnologies.SimpleSql.SqlDataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BetterTechnologies.SimpleSql
{
    public class SqlObjects: SqlBrokerBase
    {
        public SqlObjects(SqlBrokerOptions options) : base(options) { }

        public void Update(object obj)
        {
            var o = new DataObject(obj);
            if (o.ProcName != null)
                throw new Exception("Update not implemented for stored procedures");

            var sqlCmd = new SqlCommand() { Connection = GetConnection() };
            var sqlParams = new SqlParameters(sqlCmd.Parameters, obj);
            var props = o.Properties.Where(p => !p.Identity);

            UpdateQuery qry = new UpdateQuery(o.TableName);
            qry.SetValuePairs = props.Select(p => new SqlValuePair(p.Alias, sqlParams.Add(p))).ToList();

            var pk = o.PrimaryKeyProperty;

            if (pk == null)
                throw new Exception("Cannot update tables without a primary key.");

            qry.WhereValuePairs = new List<SqlValuePair>() { new SqlValuePair(pk.Alias, sqlParams.Add(pk.GetValue(obj))) };
            sqlCmd.CommandText = qry.ToString();
            ExecuteSqlCommand(sqlCmd);
        }

        public void Insert(object obj)
        {
            var o = new DataObject(obj);
            if (o.ProcName != null)
                throw new Exception("Insert not implemented for stored procedures");

            var sqlCmd = new SqlCommand() { Connection = GetConnection() };
            var sqlParams = new SqlParameters(sqlCmd.Parameters, obj);
            var props = o.Properties.Where(p => !p.Identity);

            InsertQuery qry = new InsertQuery(o.TableName);
            qry.FieldValuePairs = props.Select(p => new SqlValuePair(p.Alias, sqlParams.Add(p))).ToList();

            bool hasIdent = o.IdentityProperty != null;
            if (hasIdent)
                qry.IdentityField = o.IdentityProperty.Name;
            sqlCmd.CommandText = qry.ToString();

            if (hasIdent)
            {
                int x = (int)ExecuteSqlScalar(sqlCmd);
                o.IdentityProperty.SetValue(obj, x);
            }
            else
                ExecuteSqlCommand(sqlCmd);
        }

        public void Delete(object obj)
        {
            var o = new DataObject(obj);
            if (o.ProcName != null)
                throw new Exception("Delete not implemented for stored procedures");

            var sqlCmd = new SqlCommand() { Connection = GetConnection() };
            var sqlParams = new SqlParameters(sqlCmd.Parameters, obj);

            DeleteQuery qry = new DeleteQuery(o.TableName);

            var pk = o.PrimaryKeyProperty;
            qry.Where = new List<SqlValuePair>() { new SqlValuePair(pk.Alias, sqlParams.Add(pk.GetValue(obj))) };

            sqlCmd.CommandText = qry.ToString();
            ExecuteSqlCommand(sqlCmd);
        }


        public GridViewResults<T> LoadGridView<T>(GridViewOptions options)
        {
            var o = new DataObject(typeof(T));
            var sqlCmd = new SqlCommand() { Connection = GetConnection(true) };

            var sQry = new SelectQuery(o.TableName);
            sQry.Distinct = options.Distinct;
            sQry.IncludeTotalRowCount = true;
            sQry.OrderByValues = options.OrderBy;
            sQry.RowOffset = options.RowOffset;
            sQry.PageSize = options.PageSize;
            sQry.Fields = o.Properties.Select(p => p.Alias).ToList();
            sQry.GlobalSearchValue = options.GlobalSearch;

            var filters = options.QueryFilters;
            if (filters != null && filters.Count() > 0)
            {
                var sqlParams = new SqlParameters(sqlCmd.Parameters);
                var obj = Activator.CreateInstance(o.Type);

                foreach (var f in filters)
                { // Use the alias if there is one, or if not use the property name
                    if (o.Properties[f.Property] != null)
                        f.Alias = o.Properties[f.Property].Alias;
                    else
                        f.Alias = f.Property;
                }

                sQry.WhereValuePairs = filters.Select(p => new SqlValuePair(p.Alias, sqlParams.Add(p.Value))).ToList();
            }

            sqlCmd.CommandText = sQry.ToString();
            int totalRowCount = 0;
            var results = new List<T>();

            using (var dr = sqlCmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                while (dr.Read())
                {
                    if (totalRowCount == 0)
                        totalRowCount = (int)dr["TotalRowCount"];

                    results.Add((T)FillObject(dr, o.Type, o.Properties));
                }

            return new GridViewResults<T>(results, totalRowCount);
        }

        public List<T> Load<T>(QueryFilters filter = null)
        {
            var o = new DataObject(typeof(T));
            var sqlCmd = GetLoadCommand(o, filter);
            var results = new List<T>();

            using (var dr = sqlCmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                while (dr.Read())
                    results.Add((T)FillObject(dr, o.Type, o.Properties));

            return results;
        }

        SqlCommand GetLoadCommand(DataObject o, QueryFilters filter = null)
        {
            var sqlCmd = new SqlCommand() { Connection = GetConnection(true) };

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

        public int Count<T>()
        {
            var o = new DataObject(typeof(T));
            var sqlCmd = new SqlCommand() { Connection = GetConnection(true) };
            var sQry = new CountQuery(o.TableName);

            sqlCmd.CommandText = sQry.ToString();
            var count = sqlCmd.ExecuteScalar();
            return (int)count;
        }

        public void BulkCopy<T>(IEnumerable<T> data, string destinationTableName, params string[] fieldNames)
        {
            DataTable table = new DataTable();

            using (var reader = ObjectReader.Create(data, fieldNames))
                table.Load(reader);

            using (var destinationConnection = GetConnection())
            {
                destinationConnection.Open();
                var trx = destinationConnection.BeginTransaction();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destinationConnection, SqlBulkCopyOptions.TableLock, trx))
                {
                    bulkCopy.DestinationTableName = destinationTableName;

                    foreach (var f in fieldNames)
                        bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(f, f));

                    try { bulkCopy.WriteToServer(table); }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }

                trx.Commit();
            }
        }
    }
}
