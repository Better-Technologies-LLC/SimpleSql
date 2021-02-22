using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace BetterTechnologies.SimpleSql.SqlDataAccess
{
    public class QueryFilters : IEnumerable<QueryFilters.Filter>
    {
        protected List<Filter> _filters = new List<Filter>();

        public QueryFilters() { }
        public QueryFilters(string property, object value) { Add(property, value); }

        public QueryFilters(Dictionary<string, string> searchDictionary)
        {
            foreach (KeyValuePair<string, string> kvp in searchDictionary)
                Add(kvp.Key, kvp.Value);
        }

        public class Filter
        {
            public string Property;
            public string Alias;
            public object Value;
        }

        public void Add(string property, object value)
        {
            _filters.Add(new Filter() { Property = property, Value = value });
        }

        public IEnumerator<QueryFilters.Filter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _filters.GetEnumerator();
        }
    }
}
