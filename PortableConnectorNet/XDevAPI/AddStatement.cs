using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Serialization;


namespace MySql.XDevAPI
{
  public class AddStatement : CrudStatement
  {
    private List<string> _jsonDocs = new List<string>();

    internal AddStatement(Collection collection) : base(collection)
    {
    }

    public void Add(params object[] items)
    {
      _jsonDocs.AddRange(JsonSerializer.ToJson(items));
    }

    public void Add(params string[] items)
    {
      _jsonDocs.AddRange(JsonSerializer.EnsureId(items));
    }

    public override DocumentResult Execute()
    {
      return Collection.Schema.Session.XSession.Insert(Collection, _jsonDocs.ToArray());
    }

  }
}
