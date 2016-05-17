using Microsoft.Data.Entity.Metadata;

namespace MySQL.Data.Entity.Metadata
{
    public class MySQLModelAnnotations : RelationalModelAnnotations
    {
        public MySQLModelAnnotations( IModel model )
            :  base(model, MySQLAnnotationNames.Prefix)
        {

        }
    }
}
