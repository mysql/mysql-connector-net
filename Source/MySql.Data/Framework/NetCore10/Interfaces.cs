using System;

namespace MySql.Data.MySqlClient.Common
{
  interface ICloneable
  {

  }

  internal class CategoryAttribute : Attribute
  {
    public CategoryAttribute(string s) { }
  }

  internal class DescriptionAttribute : Attribute
  {
    public DescriptionAttribute(string s) { }
  }

  internal class BrowsableAttribute : Attribute
  {
    public BrowsableAttribute(bool b){ }
  }

  internal class DbProviderSpecificTypePropertyAttribute : Attribute
  {
    public DbProviderSpecificTypePropertyAttribute(bool b) { }
  }

  internal class DesignerSerializationVisibilityAttribute : Attribute
  {
    public DesignerSerializationVisibilityAttribute(object c) { }
  }

  internal enum DesignerSerializationVisibility {  Content }
}

namespace MySql.Data.MySqlClient.Replication
{
}
