using System;
using System.Collections.Generic;
using System.Text;

namespace BetterTechnologies.SimpleSql
{
    public class SqlBrokerOptions
    {
        public string ConnectionKey { get; set; }
        public string ConnectionString { get; set; }
        public bool ImplicitMapping { get; set; }
    }
}
