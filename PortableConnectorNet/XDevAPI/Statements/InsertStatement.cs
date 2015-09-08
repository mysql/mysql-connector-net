using MySql.XDevAPI.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI.Statements
{
  public class InsertStatement : BaseStatement<Table, UpdateResult>
  {
    internal string[] fields;
    internal List<object[]> values = new List<object[]>();
    internal object[] parameters;

    public InsertStatement(Table table, string[] fields) : base(table)
    {
      this.fields = fields;
    }

    public override UpdateResult Execute()
    {

      var result = CollectionOrTable.Session.XSession.InsertRows(this);
      if(result.Succeeded) values = null;
      return result;
    }

    public InsertStatement Values(params object[] values)
    {
      this.values.Add(values);
      return this;
    }

    internal InsertStatement Bind(params object[] parameters)
    {
      this.parameters = parameters;
      return this;
    }
  }
}
