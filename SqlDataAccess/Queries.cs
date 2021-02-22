using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterTechnologies.SimpleSql.SqlDataAccess
{
    public class CountQuery: SelectQuery
    {
        public CountQuery(string tableName) : base(null, tableName) { }
        
        public CountQuery(string databaseName, string tableName): base(databaseName, tableName) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select count(*) from ");

            if (DatabaseName != null)
            {
                sb.Append(DatabaseName + ".");

                // TableName may include schema, but if not...
                if (!TableName.Contains("."))
                    sb.Append(".");
            }

            sb.Append(TableName);
            return sb.ToString();
        }
    }

    public class SelectQuery
    {
        public SelectQuery(string tableName): this(null, tableName) { }

        public SelectQuery(string databaseName, string tableName)
        {
            DatabaseName = databaseName; 
            TableName = tableName;
            Fields = new List<string>();
            WhereValuePairs = new List<SqlValuePair>();
            GroupByValues = new List<string>();
            HavingValuePairs = new List<SqlValuePair>();
            OrderByValues = new List<SortField>();
        }

        public bool IncludeTotalRowCount { get; set; }
        public bool Distinct { get; set; }
        public int Top { get; set; }

        // Uses OFFSET...FETCH NEXT for paging
        public int RowOffset { get; set; }
        public int PageSize { get; set; }

        public IList<string> Fields { get; set; }
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IList<SqlValuePair> WhereValuePairs { get; set; }
        public IList<string> GroupByValues { get; set; }
        public IList<SqlValuePair> HavingValuePairs { get; set; }
        public IList<SortField> OrderByValues { get; set; }
        public string GlobalSearchValue { get; set; }

        public void AddSortField(string fieldName, bool descending = false)
        {
            OrderByValues.Add(new SortField(fieldName, descending));
        }

        public override string ToString()
        {
            var fields = Fields == null ? "*" : string.Join(", ", Fields.ToArray());

            var where = WhereValuePairs == null || WhereValuePairs.Count == 0 ? null :
                string.Join(" and ", WhereValuePairs.Select(o => o.ToCondition()).ToArray());


            string globalSearch = string.IsNullOrEmpty(GlobalSearchValue) ? null : 
                string.Join(" or ", Fields.Select(o => o + " like '%" + GlobalSearchValue + "%'").ToArray());
            
            var order = OrderByValues == null || OrderByValues.Count == 0 ? null :
                string.Join(", ", OrderByValues.Select(o => o.ToString()).ToArray());

            var group = GroupByValues == null || GroupByValues.Count == 0 ? null :
                string.Join(", ", GroupByValues.ToArray());

            var having = HavingValuePairs == null || HavingValuePairs.Count == 0 ? null :
                string.Join(" and ", HavingValuePairs.Select(o => o.ToCondition()).ToArray());

            StringBuilder sb = new StringBuilder();
            if (IncludeTotalRowCount)
                sb.Append("with TempResult as (");

            sb.Append("select " + (Distinct ? "distinct " : null));
            if (Top > 0) sb.Append(" top " + Top + " ");
            sb.Append(fields == null || fields.Count() == 0 ? "*" : fields);

            sb.Append(" from ");

            if (DatabaseName != null)
            {
                sb.Append(DatabaseName + ".");

                // TableName may include schema, but if not...
                if (!TableName.Contains("."))
                    sb.Append(".");
            }

            sb.Append(TableName);
            
            if (where != null || globalSearch != null) sb.Append(" where ");
            if(where != null)  sb.Append("(" + where + ")");
            if (globalSearch != null && where != null) sb.Append(" and ");
            if(globalSearch != null) sb.Append("(" + globalSearch + ")");
            
            if (group != null) sb.Append(" group by " + group);

            if (IncludeTotalRowCount)
                sb.Append("), TempCount as (select count(*) as TotalRowCount from TempResult) select * from TempResult, TempCount");
            
            if (order != null) sb.Append(" order by " + order);
            
            if (RowOffset > 0 || PageSize > 0)
                sb.Append(" offset " + RowOffset + " rows fetch next " + PageSize + " rows only");
            
            return sb.ToString();
        }
    }

    public class UpdateQuery
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IList<SqlValuePair> SetValuePairs { get; set; }
        public IList<SqlValuePair> WhereValuePairs { get; set; }

        public UpdateQuery(string tableName) { TableName = tableName; }
        public UpdateQuery(string databaseName, string tableName) { DatabaseName = databaseName; TableName = tableName; }

        public override string ToString()
        {
            var set = string.Join(",", SetValuePairs.Select(o => o.ToAssignment()).ToArray());
            var where = string.Join(" and ", WhereValuePairs.Select(o => o.ToCondition()).ToArray());

            StringBuilder sb = new StringBuilder();
            sb.Append("update ");
            
            if(DatabaseName != null) 
                sb.Append(DatabaseName + "..");
            
            sb.Append(TableName);
            sb.Append(" set " + set);
            sb.Append(" where " + where);
            return sb.ToString();
        }
    }

    public class InsertQuery
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IList<SqlValuePair> FieldValuePairs { get; set; }
        public string IdentityField { get; set; }

        public InsertQuery(string tableName) { TableName = tableName; }
        public InsertQuery(string databaseName, string tableName) { DatabaseName = databaseName; TableName = tableName; }

        public override string ToString()
        {
            var fields = string.Join(", ", FieldValuePairs.Select(o => o.Field).ToArray());
            var values = string.Join(", ", FieldValuePairs.Select(o => o.ParameterNumber).ToArray());

            StringBuilder sb = new StringBuilder();
            sb.Append("insert ");
            
            if(DatabaseName != null)
                sb.Append(DatabaseName + "..");
            
            sb.Append(TableName);
            sb.Append(" (" + fields + ")");
            if (IdentityField != null) sb.Append(" OUTPUT INSERTED." + IdentityField + " Ident");
            sb.Append(" values (" + values + ")");
            return sb.ToString();
        }
    }

    public class DeleteQuery
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IList<SqlValuePair> Where { get; set; }

        public DeleteQuery(string tableName) { TableName = tableName; }
        public DeleteQuery(string databaseName, string tableName) { DatabaseName = databaseName; TableName = tableName; }

        public override string ToString()
        {
            var where = string.Join(" and ", Where.Select(o => o.ToCondition()).ToArray());
            return "delete " + (DatabaseName == null ? "" : DatabaseName + "..") + TableName + " where " + where;
        }
    }

    public class SqlValuePair
    {
        public string Field { get; private set; }
        public string ParameterNumber { get; private set; }
        public string Comparison { get; set; }

        public SqlValuePair(string fieldName, string value, string comparison = "=")
        {
            Field = fieldName;
            ParameterNumber = value;
            Comparison = comparison;
        }

        public string ToAssignment()
        {
            return Field + "=" + ParameterNumber;
        }

        public string ToCondition()
        {
            return Field + (ParameterNumber == null ? " is null" : " " + Comparison + " " + ParameterNumber);
        }
    }

    public class SortField
    {
        public SortField(string fieldName, bool descending = false)
        {
            FieldName = fieldName;
            Descending = descending;
        }

        public string FieldName { get; set; }
        public bool Descending { get; set; }

        public override string ToString()
        {
            return FieldName + (Descending ? " desc" : null);
        }
    }

    public class GridViewOptions
    {
        public bool Distinct { get; set; }
        public int RowOffset { get; set; }
        public int PageSize { get; set; }
        public string GlobalSearch { get; set; }
        public List<SortField> OrderBy { get; set; }
        public QueryFilters QueryFilters { get; set; }

        public GridViewOptions(int rowOffset, int pageSize, List<SortField> orderBy, QueryFilters filters, string globalSearch = null)
        {
            if (orderBy == null || orderBy.Count == 0)
                throw new Exception("You must provide at least one field to order by.");

            QueryFilters = filters;
            RowOffset = rowOffset;
            PageSize = pageSize;
            OrderBy = orderBy;
            GlobalSearch = globalSearch;
        }
    }
}
