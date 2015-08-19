using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests
{
    public class CollectionTests : BaseTest
    {
        [Fact]
        public void GetAllCollections()
        {
            Session s = GetSession();
            Schema schema = s.GetDefaultSchema();
            List<Collection> collections = schema.GetCollections();
        }
    }
}
