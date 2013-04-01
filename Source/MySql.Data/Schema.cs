using MySql.Data.MySqlClient.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;

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

#if !RT
    internal DataTable AsDataTable()
    {
      DataTable dt = new DataTable(Name);
      foreach (SchemaColumn col in Columns)
        dt.Columns.Add(col.Name, col.Type);
      foreach (MySqlSchemaRow row in Rows)
      {
        DataRow newRow = dt.NewRow();
        for (int i = 0; i < dt.Columns.Count; i++)
          newRow[i] = row[i];
        dt.Rows.Add(newRow);
      }
      return dt;
    }
#endif
  }

  public class MySqlSchemaRow : List<object>
  {
    public MySqlSchemaRow(MySqlSchemaCollection c)
    {
      Collection = c;
    }

    internal MySqlSchemaCollection Collection { get; private set; }

    internal object this[string s]
    {
      get { return GetValueForName(s); }
      set { SetValueForName(s, value); }
    }

    private void SetValueForName(string colName, object value)
    {
      int index = Collection.Mapping[colName];
      this[index] = value;
    }

    private object GetValueForName(string colName)
    {
      int index = Collection.Mapping[colName];
      return this[index];
    }

    internal void CopyRow(MySqlSchemaRow row)
    {
      if (Collection.Columns.Count != row.Collection.Columns.Count)
        throw new InvalidOperationException("column count doesn't match");
      for (int i = 0; i < Collection.Columns.Count; i++)
        row[i] = this[i];
    }
  }

  public class SchemaColumn
  {
    public string Name { get; set; }
    public Type Type { get; set; }
  }
}
