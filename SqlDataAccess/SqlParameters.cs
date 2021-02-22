using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace BetterTechnologies.SimpleSql.SqlDataAccess
{
    public class SqlParameters
    {
        public SqlParameterCollection Items { get; private set; }
        object _obj;

        public int ParameterCount { get; private set; }

        public SqlParameters(SqlParameterCollection sqlParams, object obj = null)
        {
            Items = sqlParams;
            _obj = obj;
            ParameterCount = 1;
        }

        public string Add(object value)
        {
            Items.Add(new SqlParameter(ParameterCount.ToString(), value ?? DBNull.Value));
            return "@" + ParameterCount++.ToString();
        }

        internal string Add(DataObjectProperty property)
        {
            var value = property.GetValue(_obj);
            return Add(value);
        }
    }
}
