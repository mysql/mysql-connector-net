## MySQL Connector/NET and X DevAPI

MySQL Connector/NET 8.3 supports X Protocol, which enables you to use X DevAPI with the .NET language of choice to develop applications that communicate with a MySQL server functioning as a document store, relational database, or both.

To get started, review the following main classes:

* [MySQLX](api/data_api/MySqlX.XDevAPI.MySQLX.yml): Features static methods for creating sessions using the X Protocol.
* [Session](api/data_api/MySqlX.XDevAPI.Session.yml): Represents a server session.
* [DbDoc](api/data_api/MySqlX.XDevAPI.DbDoc.yml): Represents a generic document.
* [Collection](api/data_api/MySqlX.XDevAPI.Collection.yml): Represents a collection of documents.
* [Schema](api/data_api/MySqlX.XDevAPI.Schema.yml): Represents a schema or database.
* [Result](api/data_api/MySqlX.XDevAPI.Common.Result.yml): Represents a general statement result.
* [DocResult](api/data_api/MySqlX.XDevAPI.CRUD.DocResult.yml): Represents the result of an operation that includes a collection of documents.

For an introduction to X DevAPI concepts, see the [X DevAPI User Guide](https://dev.mysql.com/doc/x-devapi-userguide/en/). For general information about MySQL Connector/NET, see the [MySQL Connector/NET Developer Guide](https://dev.mysql.com/doc/connector-net/en/).

## MySQL Connector/NET

MySQL Connector/NET 8.3 also supports the development of .NET, .NET Core and .NET Framework applications that require secure, high-performance data connectivity with MySQL through the classic protocol. It supports ADO.NET, Entity Framework and various web providers.

To get started, review the following main classes:

* [MySqlConnection](api/data_api/MySql.Data.MySqlClient.MySqlConnection.yml): Represents an open connection to a MySQL database.
* [MySqlConnectionStringBuilder](api/data_api/MySql.Data.MySqlClient.MySqlConnectionStringBuilder.yml): Aids in the creation of a connection string by exposing the connection options as properties.
* [MySqlCommand](api/data_api/MySql.Data.MySqlClient.MySqlCommand.yml): Represents an SQL statement to execute against a MySQL database.
* [MySqlCommandBuilder](api/data_api/MySql.Data.MySqlClient.MySqlCommandBuilder.yml): Automatically generates single-table commands used to reconcile changes made to a DataSet object with the associated MySQL database.
* [MySqlDataAdapter](api/data_api/MySql.Data.MySqlClient.MySqlDataAdapter.yml): Represents a set of data commands and a database connection that are used to fill a data set and update a MySQL database.
* [MySqlDataReader](api/data_api/MySql.Data.MySqlClient.MySqlDataReader.yml): Provides a means of reading a forward-only stream of rows from a MySQL database.
* [MySqlException](api/data_api/MySql.Data.MySqlClient.MySqlException.yml): The exception that is thrown when MySQL returns an error.
* [MySqlHelper](api/data_api/MySql.Data.MySqlClient.MySqlHelper.yml): Helper class that makes it easier to work with the provider.
* [MySqlTransaction](api/data_api/MySql.Data.MySqlClient.MySqlTransaction.yml): Represents an SQL transaction to be made in a MySQL database.
* [MySQLMembershipProvider](api/web_api/MySql.Web.Security.MySQLMembershipProvider.yml): Manages storage of membership information for an ASP.NET application in a MySQL database.
* [MySQLRoleProvider](api/web_api/MySql.Web.Security.MySQLRoleProvider.yml): Manages storage of role membership information for an ASP.NET application in a MySQL database.
* [MySqlEFConfiguration](api/ef_api/MySql.Data.EntityFramework.MySqlEFConfiguration.yml): Adds the dependency resolvers for MySQL classes.
* [MySqlExecutionStrategy](api/ef_api/MySql.Data.EntityFramework.MySqlExecutionStrategy.yml): Enables automatic recovery from transient connection failures.

For additional information about MySQL Connector/NET, see the [MySQL Connector/NET Developer Guide](https://dev.mysql.com/doc/connector-net/en/).

### Additional Resources

* [MySQL](http://www.mysql.com/)
* [Discussion Forum](https://forums.mysql.com/list.php?38)
* [MySql Bugs database](https://bugs.mysql.com)
* For more information about this and other MySQL products, please visit [MySQL Contact & Questions](http://www.mysql.com/about/contact/).

[![Twitter Follow](https://img.shields.io/twitter/follow/MySQL.svg?label=Follow%20%40MySQL&style=social)](https://twitter.com/intent/follow?screen_name=MySQL)