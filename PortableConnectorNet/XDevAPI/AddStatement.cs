using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Serialization;


namespace MySql.XDevAPI
{
  public class AddStatement : CrudStatement
  {
    private List<JsonDoc> _jsonDocs = new List<JsonDoc>();

    internal AddStatement(Collection collection) : base(collection)
    {
    }

    public void Add(params object[] items)
    {
      _jsonDocs.AddRange(GetDocs(items, true));
    }

    public void Add(params string[] items)
    {
      _jsonDocs.AddRange(GetDocs(items, true));
    }

    public override DocumentResult Execute()
    {
      return Collection.Schema.Session.XSession.Insert(Collection, _jsonDocs.ToArray());
    }

  }
}
