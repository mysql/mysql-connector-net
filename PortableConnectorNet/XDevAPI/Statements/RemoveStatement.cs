using System;


namespace MySql.XDevAPI.Statements
{
  public class RemoveStatement : FilterableStatement<RemoveStatement>
  {
    internal RemoveStatement(Collection collection, string condition) : base(collection, condition)
    {
    }

    public override DocumentResult Execute()
    {
      return Collection.Schema.Session.XSession.DeleteDocs(this);
    }
  }
}
