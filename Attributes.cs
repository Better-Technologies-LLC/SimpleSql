using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterTechnologies.SimpleSql
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DatabaseAttribute : Attribute
    {
        public readonly string Name;
        public DatabaseAttribute() { }
        public DatabaseAttribute(string name) { Name = name; }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public readonly string Name;
        public TableAttribute() { }
        public TableAttribute(string name) { Name = name; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ProcAttribute : Attribute
    {
        public readonly string Name;
        public ProcAttribute() { }
        public ProcAttribute(string name) { Name = name; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicAttribute : Attribute
    {
        public readonly string Name;
        public DynamicAttribute() { }
        public DynamicAttribute(string name) { Name = name; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public readonly string Name;
        public FieldAttribute() { }
        public FieldAttribute(string name) { Name = name; }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IdentityAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class TimeStampAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class RowVersionAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class KeyAttribute : Attribute
    {
        public readonly string KeyName;
        public readonly string Alias;

        public KeyAttribute(string keyName, string alias)
        {
            KeyName = keyName;
            Alias = alias;
        }
    }
}
