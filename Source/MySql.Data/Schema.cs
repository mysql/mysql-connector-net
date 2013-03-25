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
    public ICollection Columns { get { return columns; } }
    public ICollection Rows { get { return rows; } }

    public SchemaColumn AddColumn(string name, Type t)
    {
      SchemaColumn c = new SchemaColumn();
      c.Name = name;
      c.Type = t;
      columns.Add(c);
      Mapping.Add(name, columns.Count-1);
      return c;
    }

    public MySqlSchemaRow AddRow()
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

    public object this[string s]
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
