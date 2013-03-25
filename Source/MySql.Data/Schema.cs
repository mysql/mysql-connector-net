using MySql.Data.MySqlClient.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  public class MySqlSchemaCollection 
  {
    private List<SchemaColumn> columns = new List<SchemaColumn>();
    private List<MySqlSchemaRow> rows = new List<MySqlSchemaRow>();

    public MySqlSchemaCollection()
    {
      Mapping = new Dictionary<string,int>();
    }

    public MySqlSchemaCollection(string name) : this()
    {
      Name = name;
    }

    internal Dictionary<string, int> Mapping;
    public string Name { get; set; }
    public IList<SchemaColumn> Columns { get { return columns; } }
    public IList<MySqlSchemaRow> Rows { get { return rows; } }

    internal SchemaColumn AddColumn(string name, Type t)
    {
      SchemaColumn c = new SchemaColumn();
      c.Name = name;
      c.Type = t;
      columns.Add(c);
      Mapping.Add(name, columns.Count-1);
      return c;
    }

    internal int ColumnIndex(string name)
    {
      int index = -1;
      for (int i = 0; i < columns.Count; i++)
      {
        SchemaColumn c = columns[i];
        if (String.Compare(c.Name, name, StringComparison.OrdinalIgnoreCase) != 0) continue;
        index = i;
        break;
      }
      return index;
    }

    internal void RemoveColumn(string name)
    {
      int index = ColumnIndex(name);
      if (index == -1)
        throw new InvalidOperationException();
      columns.RemoveAt(index);
      foreach (MySqlSchemaRow row in rows)
        row.RemoveAt(index);
    }

    internal bool ContainsColumn(string name)
    {
      return ColumnIndex(name) >= 0;
    }

    internal MySqlSchemaRow AddRow()
    {
      MySqlSchemaRow r = new MySqlSchemaRow(this);
      rows.Add(r);
      return r;
    }
  }

  public class MySqlSchemaRow : List<object>
  {
    private MySqlSchemaCollection collection;

    public MySqlSchemaRow(MySqlSchemaCollection c)
    {
      collection = c;
    }

    internal object this[string s]
    {
      get { return GetValueForName(s); }
      set { SetValueForName(s, value); }
    }

    private void SetValueForName(string colName, object value)
    {
      int index = collection.Mapping[colName];
      this[index] = value;
    }

    private object GetValueForName(string colName)
    {
      int index = collection.Mapping[colName];
      return this[index];
    }
  }

  public class SchemaColumn
  {
    public string Name { get; set; }
    public Type Type { get; set; }
  }
}
