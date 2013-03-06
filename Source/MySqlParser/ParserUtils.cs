using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Parser
{
  public static class ParserUtils
  {
    public static TableWithAlias ExtractTableName(ITree child)
    {
      string alias = "";
      string table = "";
      string db = "";

      switch (child.ChildCount)
      {
        case 1:
          table = child.GetChild(0).Text;
          break;
        case 2:
          if (string.Compare(child.GetChild(1).Text, "alias", true) == 0)
          {
            table = child.GetChild(0).Text;
            alias = child.GetChild(1).GetChild(0).Text;
          }
          else
          {
            db = child.GetChild(0).Text;
            table = child.GetChild(1).Text;
          }
          break;
        case 3:
          db = child.GetChild(0).Text;
          table = child.GetChild(1).Text;
          alias = child.GetChild(2).GetChild(0).Text;
          break;
      }
      return new TableWithAlias(db, table, alias);
    }

    public static void GetTables(ITree ct, List<TableWithAlias> tables)
    {
      for (int i = 0; i < ct.ChildCount; i++)
      {
        ITree child = ct.GetChild(i);
        if (child.Text.Equals( "table_ref", StringComparison.OrdinalIgnoreCase ))
        {
          tables.Add( ExtractTableName( child ) );
        }
        else GetTables(child, tables);
      }
    }

    public static Version GetVersion(string versionString)
    {
      Version version;
      int i = 0;
      while (i < versionString.Length &&
          (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
        i++;
      version = new Version(versionString.Substring(0, i));
      return version;
    }
  }

  public class TableWithAlias : IEquatable<TableWithAlias>
  {
    public TableWithAlias(string TableName)
      : this("", TableName, "")
    {
    }

    public TableWithAlias(string TableName, string Alias)
      : this("", TableName, Alias)
    {
    }

    public TableWithAlias(string Database, string TableName, string Alias)
    {
      this.Database = Database.Replace("`", "").ToLower();
      this.TableName = TableName.Replace("`", "").ToLower();
      this.Alias = Alias.Replace("`", "");
    }

    public string TableName { get; private set; }
    public string Alias { get; private set; }
    public string Database { get; private set; }

    public bool Equals(TableWithAlias other)
    {
      if (other == null) return false;      
      return
        (other.TableName.Equals(this.TableName, StringComparison.CurrentCultureIgnoreCase)) &&
        (other.Alias.Equals(this.Alias, StringComparison.CurrentCultureIgnoreCase)) &&
        (other.Database.Equals(this.Database, StringComparison.CurrentCultureIgnoreCase));
    }
  }
}
