using System;


namespace MySql.XDevAPI
{
  public class RemoveStatement : FilterableStatement<RemoveStatement>
  {
    internal RemoveStatement(Collection collection, string condition) : base(collection)
    {
      this.Where(condition);
    }

    public override DocumentResult Execute()
    {
      return Collection.Schema.Session.XSession.DeleteDocs(this);
    }
  }
}
