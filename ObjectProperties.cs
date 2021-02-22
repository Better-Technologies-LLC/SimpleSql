using Fasterflect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterTechnologies.SimpleSql
{
    public class ObjectProperty
    {
        public PropertyInfo Property { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }

        MemberGetter _getter;
        MemberSetter _setter;

        public ObjectProperty(PropertyInfo property)
        {
            Property = property;
            Name = property.Name;
            Type = property.PropertyType.ToString();
        }

        internal void CachePropertyAccessors<T>()
        {
            _getter = typeof(T).DelegateForGetPropertyValue(Name);
            _setter = typeof(T).DelegateForSetPropertyValue(Name);
        }

        internal void CachePropertyAccessors(Type type)
        {
            _getter = type.DelegateForGetPropertyValue(Name);
            _setter = type.DelegateForSetPropertyValue(Name);
        }

        internal void CachePropertyGetAccessor(Type type)
        {
            _getter = type.DelegateForGetPropertyValue(Name);
        }

        public object GetValue(object obj) { return _getter(obj); }
        public void SetValue(object obj, object value) { _setter(obj, value); }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ObjectProperties : IEnumerable<ObjectProperty>
    {
        List<ObjectProperty> _props = new List<ObjectProperty>();

        public void Add(ObjectProperty property)
        {
            _props.Add(property);
        }

        public ObjectProperty this[string propertyName]
        {
            get
            {
                return _props.FirstOrDefault(o => o.Name == propertyName);
            }
        }

        public IEnumerator<ObjectProperty> GetEnumerator() { return _props.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _props.GetEnumerator(); }
    }

    public class DataObjectProperty : ObjectProperty
    {
        public string Alias { get; set; }
        public bool Identity { get; set; }
        public bool PrimaryKey { get; set; }
        public bool TimeStamp { get; set; }

        public DataObjectProperty(PropertyInfo property, string alias)
            : base(property)
        {
            var ident = property.GetCustomAttributes(typeof(IdentityAttribute), false);
            var prKey = property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false);
            var timeStamp = property.GetCustomAttributes(typeof(TimeStampAttribute), false);

            this.Alias = alias;
            this.Identity = ident.Count() == 1;
            this.PrimaryKey = prKey.Count() == 1;
            this.TimeStamp = timeStamp.Count() == 1;
        }
    }

    public class DataObjectProperties : IEnumerable<DataObjectProperty>
    {
        List<DataObjectProperty> _props = new List<DataObjectProperty>();

        public void Add(DataObjectProperty property)
        {
            _props.Add(property);
        }

        public DataObjectProperty this[string propertyName]
        {
            get
            {
                return _props.FirstOrDefault(o => o.Name == propertyName);
            }
        }

        public IEnumerator<DataObjectProperty> GetEnumerator() { return _props.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _props.GetEnumerator(); }
    }
}
