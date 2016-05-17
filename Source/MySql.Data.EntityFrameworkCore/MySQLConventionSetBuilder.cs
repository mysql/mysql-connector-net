using Microsoft.Data.Entity.Metadata.Conventions;
using Microsoft.Data.Entity.Metadata.Conventions.Internal;

namespace MySQL.Data.Entity
{
    public class MySQLConventionSetBuilder : IConventionSetBuilder
    {
        public ConventionSet AddConventions(ConventionSet conventionSet)
        {
            ThrowIf.Argument.IsNull(conventionSet, "conventionSet");

            return conventionSet;
        }
    }
}
