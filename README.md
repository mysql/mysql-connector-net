# MySQL Connector/NET

MySQL provides connectivity for client applications developed in .NET compatible programming languages with Connector/NET.

MySQL Connector/NET is a library compatible with .NET Framework and .NET Core, for specific versions see [MySQL Connector/NET Versions](https://dev.mysql.com/doc/connector-net/en/connector-net-versions.html). The driver is a pure C# implementation of the MySQL protocol and does not rely on the MySQL client library.

From MySQL Connector/NET 8.0, the driver also containt an implementation of [MySQL X DevAPI](https://dev.mysql.com/doc/x-devapi-userguide/en/), an Application Programming Interface for working with [MySQL as a Document Store](https://dev.mysql.com/doc/refman/8.0/en/document-store.html) through CRUD-based, NoSQL operations.

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
* [API Reference](https://dev.mysql.com/doc/dev/connector-net/8.0/html/connector-net-x-devapi-reference.htm)
* [Discussion Forum](https://forums.mysql.com/list.php?38)
* [MySql Bugs database](https://bugs.mysql.com)
* For more information about this and other MySQL products, please visit [MySQL Contact & Questions](http://www.mysql.com/about/contact/).

[![Twitter Follow](https://img.shields.io/twitter/follow/MySQL.svg?label=Follow%20%40MySQL&style=social)](https://twitter.com/intent/follow?screen_name=MySQL)