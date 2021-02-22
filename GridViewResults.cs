using System.Collections.Generic;

namespace BetterTechnologies.SimpleSql
{
    public class GridViewResults<T>
    {
        public IEnumerable<T> Results { get; set; }
        public int TotalRowCount { get; set; }

        public GridViewResults(IEnumerable<T> results, int totalRowCount)
        {
            Results = results;
            TotalRowCount = totalRowCount;
        }
    }
}
