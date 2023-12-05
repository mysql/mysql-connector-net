# MySQL Connector/NET

[![Languages](https://img.shields.io/github/languages/top/mysql/mysql-connector-net)](https://github.com/mysql/mysql-connector-net) [![License: GNU General Public License (GPLv2)](https://img.shields.io/badge/license-GPLv2_with_FOSS_exception-c30014.svg?style=flat)](LICENSE) [![NuGet](https://img.shields.io/nuget/v/MySql.Data)](https://www.nuget.org/profiles/MySQL)

MySQL provides connectivity for client applications developed in .NET compatible programming languages with Connector/NET.

MySQL Connector/NET is a library compatible with .NET Framework and .NET Core, for specific versions see [MySQL Connector/NET Versions](https://dev.mysql.com/doc/connector-net/en/connector-net-versions.html). The driver is a pure C# implementation of the MySQL protocol and does not rely on the MySQL client library.

From MySQL Connector/NET 8.0, the driver also contains an implementation of [MySQL X DevAPI](https://dev.mysql.com/doc/x-devapi-userguide/en/), an Application Programming Interface for working with [MySQL as a Document Store](https://dev.mysql.com/doc/refman/8.0/en/document-store.html) through CRUD-based, NoSQL operations.

From MySQL Connector/NET 8.1, the driver contains an implementation of [OpenTelemetry](https://dev.mysql.com/doc/connector-net/en/connector-net-programming-telemetry.html) which requires the use of the [MySql.Data.OpenTelemetry](https://www.nuget.org/profiles/MySQL/) Nuget package to enable the generation of telemetry data.

From MySQL Connector/NET 8.2, the driver adds support for [WebAuthn authentication](https://dev.mysql.com/doc/dev/connector-net/latest/api/data_api/MySql.Data.MySqlClient.WebAuthnActionCallback.html), the driver also adds support for [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8).

From MySQL Connector/NET 8.3, the driver also adds support for [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8) and [EFCore 8](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew) GA versions.

For detailed information please visit the official [MySQL Connector/NET documentation.](https://dev.mysql.com/doc/connector-net/en/)

## Licensing

Please refer to files [README](README) and [LICENSE](LICENSE), available in this repository, and [Legal Notices in documentation](https://dev.mysql.com/doc/connector-net/en/preface.html) for further details.

## Download & Install

MySQL Connector/NET can be installed from precompiled libraries by using MySQL installer or download the libraries itself, both can be found at [Connector/NET download page](https://dev.mysql.com/downloads/connector/net/). Also, you can get the latest stable release from the [official Nuget.org feed](https://www.nuget.org/profiles/MySQL).

* By using MySQL Installer, you just need to follow the wizard in order to obtain the precompiled library and then add it to your project.
* If you decided to download the precompiled libraries, decompress the folder and then add the library needed to your project as reference.
* If you go for NuGet, you could use the NuGet Package Manager inside Visual Studio or use the NuGet Command Line Interface (CLI).

### Building from sources

This driver can also be complied and installed from the sources available in this repository. Please refer to the documentation for [detailed instructions](https://dev.mysql.com/doc/connector-net/en/connector-net-installation-source.html) on how to do it.

### Github Repository

This repository contains the MySQL Connector/NET source code as per latest released version. You should expect to see the same content here and within the latest released Connector/NET package.

## Contributing

There are a few ways to contribute to the Connector/NET code. Please refer to the [contributing guidelines](CONTRIBUTING.md) for additional information.

### Additional Resources

* [MySQL](http://www.mysql.com/)
* [MySQL Connector/NET Developer Guide](https://dev.mysql.com/doc/connector-net/en/)
* [MySQL Connector/NET API](https://dev.mysql.com/doc/dev/connector-net/8.0/)
* [MySQL Connector/NET Discussion Forum](https://forums.mysql.com/list.php?38)
* [MySQL Public Bug Tracker](https://bugs.mysql.com)
* [`#connectors` channel in MySQL Community Slack](https://mysqlcommunity.slack.com/messages/connectors) ([Sign-up](https://lefred.be/mysql-community-on-slack/) required when not using an Oracle account)
* For more information about this and other MySQL products, please visit [MySQL Contact & Questions](http://www.mysql.com/about/contact/).

[![Twitter Follow](https://img.shields.io/twitter/follow/MySQL.svg?label=Follow%20%40MySQL&style=social)](https://twitter.com/intent/follow?screen_name=MySQL)