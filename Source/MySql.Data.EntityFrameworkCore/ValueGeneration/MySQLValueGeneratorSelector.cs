using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.ValueGeneration;

namespace MySQL.Data.Entity
{
  public class MySQLValueGeneratorSelector : RelationalValueGeneratorSelector
  {
    private MySQLConnection _connection;
    private MySQLSequenceValueGeneratorFactory _sequenceFactory;

    public MySQLValueGeneratorSelector(
        [NotNull] MySQLValueGeneratorCache cache,
        [NotNull] MySQLSequenceValueGeneratorFactory sequenceFactory,
        [NotNull] MySQLConnection connection,
        [NotNull] IRelationalMetadataExtensionProvider relationalExtensions)
            : base(cache, relationalExtensions)
    {
      ThrowIf.Argument.IsNull(sequenceFactory, "sequenceFactory");
      ThrowIf.Argument.IsNull(connection, "connection");

      _sequenceFactory = sequenceFactory;
      _connection = connection;
    }

  }
}