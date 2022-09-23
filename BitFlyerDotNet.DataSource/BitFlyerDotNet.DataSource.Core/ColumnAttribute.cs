using System;

namespace BitFlyerDotNet.DataSource;

public enum SortOrder
{
    None,
    Ascending,
    Descending,
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class ColumnAttribute : Attribute
{
    public string Name { get; set; }
    public bool PrimaryKey { get; set; }
    public bool Index { get; set; }
    public int IndexOrder { get; set; }
    public SortOrder SortOrder { get; set; }
    public bool EnumMember { get; set; }

    public ColumnAttribute()
    {

    }

    public ColumnAttribute(string name)
    {

    }
}
