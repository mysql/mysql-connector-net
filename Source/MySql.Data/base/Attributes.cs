using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class BrowsableAttribute : Attribute
  {
    public BrowsableAttribute(bool value)
    {
    }
  }

  internal class CategoryAttribute : Attribute
  {
    public CategoryAttribute(string s)
    {
    }
  }

  internal class DescriptionAttribute : Attribute
  {
    public DescriptionAttribute(string s)
    {
    }
  }

  internal class DisplayNameAttribute : Attribute
  {
    public DisplayNameAttribute(string s)
    {
    }
  }

  internal class PasswordPropertyTextAttribute : Attribute
  {
    public PasswordPropertyTextAttribute(bool b)
    {
    }
  }

  internal class EditorAttribute : Attribute
  {
    public EditorAttribute(string s, Type t)
    {
    }
  }

  internal class RefreshPropertiesAttribute : Attribute
  {
    public RefreshPropertiesAttribute(RefreshProperties e)
    {
    }
  }

  internal class DesignerSerializationVisibilityAttribute : Attribute
  {
    public DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility x)
    {
    }
  }

  internal class ListBindableAttribute : Attribute
  {
    public ListBindableAttribute(bool b)
    {
    }
  }

  internal class ToolboxBitmapAttribute : Attribute
  {
    public ToolboxBitmapAttribute(Type t, string s)
    {
    }
  }

  internal class DesignerCategoryAttribute : Attribute
  {
    public DesignerCategoryAttribute(string s)
    {
    }
  }

  internal class ToolboxItemAttribute : Attribute
  {
    public ToolboxItemAttribute(bool b)
    {
    }
  }
}
