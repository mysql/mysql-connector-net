# MySQL Connector/NET

[![GitHub top language](https://img.shields.io/github/languages/top/mysql/mysql-connector-net)](https://github.com/mysql/mysql-connector-net) [![License: GPLv2 with FOSS exception](https://img.shields.io/badge/license-GPLv2_with_FOSS_exception-c30014)](LICENSE) [![NuGet Version](https://img.shields.io/nuget/v/MySQL.Data)](https://www.nuget.org/profiles/MySQL/)

MySQL provides connectivity for client applications developed in .NET compatible programming languages with Connector/NET.

MySQL Connector/NET is a library compatible with .NET Framework and .NET Core, for specific versions see [MySQL Connector/NET Versions](https://dev.mysql.com/doc/connector-net/en/connector-net-versions.html). The driver is a pure C# implementation of the MySQL protocol and does not rely on the MySQL client library.

From MySQL Connector/NET 8.0, the driver also contains an implementation of [MySQL X DevAPI](https://dev.mysql.com/doc/x-devapi-userguide/en/), an Application Programming Interface for working with [MySQL as a Document Store](https://dev.mysql.com/doc/refman/8.0/en/document-store.html) through CRUD-based, NoSQL operations.

From MySQL Connector/NET 8.1, the driver contains an implementation of [OpenTelemetry](https://dev.mysql.com/doc/connector-net/en/connector-net-programming-telemetry.html) which requires the use of the [MySql.Data.OpenTelemetry](https://www.nuget.org/packages/MySql.Data.OpenTelemetry/) Nuget package to enable the generation of telemetry data.

From MySQL Connector/NET 8.2, the driver adds support for [WebAuthn authentication](https://dev.mysql.com/doc/dev/connector-net/latest/api/data_api/MySql.Data.MySqlClient.WebAuthnActionCallback.html), the driver also adds support for [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/).

From MySQL Connector/NET 8.3, the driver also adds support for [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/) and [EFCore 8](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew/) GA versions.

From MySQL Connector/NET 8.4, the driver adds support for TLS1.3 and removes support for FIDO authentication plugin.

From MySQL Connector/NET 9.0, the driver removes support for .NET 7 and EF Core 7.

From MySQL Connector/NET 9.1, the driver adds support for [.NET 9](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview/) and [EF Core 9](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew/) preview versions.

For detailed information please visit the official [MySQL Connector/NET documentation](https://dev.mysql.com/doc/connector-net/en/).

## Licensing

Please refer to files [README](README) and [LICENSE](LICENSE), available in this repository, and [Legal Notices in documentation](https://dev.mysql.com/doc/connector-net/en/preface.html) for further details.

## Security

Oracle values the independent security research community and believes that responsible disclosure of security vulnerabilities helps us ensure the security and privacy of all our users. Please refer to the [security guidelines](SECURITY.md) document for additional information.

## Download & Install

MySQL Connector/NET can be installed from precompiled libraries by using MySQL installer or download the libraries itself, both can be found at [Connector/NET download page](https://dev.mysql.com/downloads/connector/net/). Also, you can get the latest stable release from the [official Nuget.org feed](https://www.nuget.org/profiles/MySQL).

* By using MySQL Installer, you just need to follow the wizard in order to obtain the precompiled library and then add it to your project.
* If you decided to download the precompiled libraries, decompress the folder and then add the library needed to your project as a reference.
* If you go for NuGet, you could use the NuGet Package Manager inside Visual Studio or use the NuGet Command Line Interface (CLI).

### Building from sources

This driver can also be complied and installed from the sources available in this repository. Please refer to the documentation for [detailed instructions](https://dev.mysql.com/doc/connector-net/en/connector-net-installation-source.html) on how to do it.

### Github Repository

This repository contains the MySQL Connector/NET source code as per latest released version. You should expect to see the same content here and within the latest released Connector/NET package.

## Contributing

We greatly appreciate feedback from our users, including bug reports and code contributions. Your input helps us improve, and we thank you for any issues you report or code you contribute. Please refer to the [contributing guidelines](CONTRIBUTING.md) document for additional information.

### Additional Resources

* [MySQL Connector/NET Developer Guide](https://dev.mysql.com/doc/connector-net/en/)
* [MySQL Connector/NET API](https://dev.mysql.com/doc/dev/connector-net/latest/)
* [MySQL NuGet](https://www.nuget.org/profiles/MySQL/)
* [MySQL Connector/NET and C#, Mono, .Net Forum](https://forums.mysql.com/list.php?38)
* [`#connectors` channel on MySQL Community Slack](https://mysqlcommunity.slack.com/messages/connectors/)  ([Sign-up](https://lefred.be/mysql-community-on-slack/) required if you do not have an Oracle account.)
* [@MySQL on X](https://x.com/MySQL/).
* [MySQL Blog](https://blogs.oracle.com/mysql/).
* [MySQL Connectors Blog archive](https://dev.mysql.com/blog-archive/?cat=Connectors%20%2F%20Languages).
* [MySQL Newsletter](https://www.mysql.com/news-and-events/newsletter/).
* [MySQL Bugs Tracking System](https://bugs.mysql.com).

For more information about this and other MySQL products, please visit [MySQL Contact & Questions](https://www.mysql.com/about/contact/).

[![X (formerly Twitter) Follow](https://img.shields.io/twitter/follow/MySQL.svg?label=Follow%20%40MySQL&style=social)](https://x.com/intent/follow?screen_name=MySQL)
