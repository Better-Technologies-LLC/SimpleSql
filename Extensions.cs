using System;
using System.Collections.Generic;
using System.Reflection;

namespace BetterTechnologies.SimpleSql.Extensions
{
    public static class Extensions
    {
        public static Dictionary<string, object> ConvertToDictionary(this object parameters)
        {
            if (parameters is Dictionary<string, object>)
                return (Dictionary<string, object>)parameters;

            var parms = new Dictionary<string, object>();

            if (parameters != null)
                foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    object value = prop.GetValue(parameters, null);

                    if (value == null)
                        value = DBNull.Value;

                    parms.Add(prop.Name, value);
                }

            return parms;
        }
    }
}
