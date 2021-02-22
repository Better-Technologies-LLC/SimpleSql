using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterTechnologies.SimpleSql
{
    public class DataObject
    {
        static Dictionary<Type, DataObject> _cache = new Dictionary<Type, DataObject>();

        Type _type;
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string TableName { get; set; }
        public string ProcName { get; set; }

        public DataObject(Type type, bool implicitMapping = false)
        {
            Type = type;

            if (_cache.ContainsKey(Type))
            {
                DataObject d = _cache[Type];
                IdentityProperty = d.IdentityProperty;
                PrimaryKeyProperty = d.PrimaryKeyProperty;
                Properties = d.Properties;
                TableName = d.TableName;
                ProcName = d.ProcName;
            }
            else
            {
                SetProperties(implicitMapping);
                _cache.Add(Type, this);
            }
        }

        public DataObject(object o) : this(o.GetType())
        {
        }

        public DataObjectProperty IdentityProperty { get; set; }
        public DataObjectProperty PrimaryKeyProperty { get; set; }
        public DataObjectProperties Properties { get; set; }

        protected void SetProperties(bool implicitMapping)
        {
            var tableAttribute = Type.GetCustomAttributes(typeof(TableAttribute), false).Cast<TableAttribute>().SingleOrDefault();

            if (tableAttribute != null)
                TableName = tableAttribute.Name;
            else
                ProcName = Type.GetCustomAttributes(typeof(ProcAttribute), false).Cast<ProcAttribute>().SingleOrDefault()?.Name;

            Properties = new DataObjectProperties();

            foreach (var p in Type.GetProperties())
            {
                if (!p.CanWrite)
                    continue; // Skip fields that can't be written to

                var fieldAttribute = p.GetCustomAttributes(typeof(FieldAttribute), false).Cast<FieldAttribute>().FirstOrDefault();
                if (fieldAttribute == null && !implicitMapping)
                    continue;  // Ignore properties that don't have the [Field] attribute.

                string aliasString = fieldAttribute?.Name ?? p.Name; // Use alias if one was specified, otherwise use property name

                var prop = new DataObjectProperty(p, aliasString);
                prop.CachePropertyAccessors(Type);
                Properties.Add(prop);

                if (prop.Identity)
                    IdentityProperty = prop;

                if (prop.PrimaryKey)
                    PrimaryKeyProperty = prop;
            }
        }

        public object this[string property]
        {
            get { return Properties[property].GetValue(this); }
            set { Properties[property].SetValue(this, value); }
        }
    }
}
