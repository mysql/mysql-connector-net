using System;


namespace MySql.XDevAPI
{
  public class RemoveStatement : CrudStatement
  {
    Collection _collection;

    internal RemoveStatement(Collection collection)
    {
      _collection = collection;
    }

    public override DocumentResult Execute()
    {
      throw new NotImplementedException();
    }
  }
}
