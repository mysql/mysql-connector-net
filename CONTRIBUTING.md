# Contributing Guidelines

We love getting feedback from our users. Bugs and code contributions are great forms of feedback and we thank you for any bugs you report or code you contribute.

## Reporting Issues

Before reporting a new bug, please [check first](https://bugs.mysql.com/search.php) to see if a similar bug exists.

Bug reports should be as complete as possible. Please try and include the following:
* Complete steps to reproduce the issue.
* Any information about platform and environment that could be specific to the bug.
* Specific version of the product you are using.
* Specific version of the server being used.
* Sample code to help reproduce the issue if possible.

## Contributing Code

Contributing to this project is easy. You just need to follow these steps:

* Make sure you have a user account at [bugs.mysql.com](bugs.mysql.com). You'll need to reference this user account when you submit your OCA (Oracle Contributor Agreement).
* Sign the Oracle OCA. You can find instructions for doing that at the [OCA Page](https://www.oracle.com/technetwork/community/oca-486395.html).
* Develop your pull request.
  * Make sure you are aware of the requirements for the project (e.g. don't require .NET Core 3.0 if we are curently supporting .NET Core 2.2 and lower).
* Validate your pull request by including tests that sufficiently cover the functionality.
* Verify that the entire test suite passes with your code applied.
* Submit your pull request. While you can submit the pull request via [Github](https://github.com/mysql/mysql-connector-net/pulls), you can also submit it directly via [bugs.mysql.com](bugs.mysql.com).

Thanks again for being willing to contribute to MySQL. We truly believe in the principles of open source and appreciate any and all contributions to our projects.

## Setting Up a Development Environment

You can use your preferred .NET IDE to view, edit, and compile the MySQL Connector/NET source code. The configuration setup can be adapted from [Installing from Source](https://dev.mysql.com/doc/connector-net/en/connector-net-installation-source.html).

## Running Tests

Any code you contribute needs to pass our test suite, each of our projects have his own (e.g. MySql.Data.Tests). You must run the entire suite just to make sure that no other functionality have been affected by the change. You can run the test suite by using the IDE of your preference or by CLI with the help of the "dotnet" tool.
 
## Getting Help

If you need help or just want to get in touch with us, please use the following resources:

* [MySQL Connector/NET and C# forum](https://forums.mysql.com/list.php?38)
* [MySQL NuGet](https://www.nuget.org/profiles/MySQL)
* [MySQL Bugs database](https://bugs.mysql.com/)

We hope to hear from you soon. Enjoy your coding!

[![Twitter Follow](https://img.shields.io/twitter/follow/MySQL.svg?label=Follow%20%40MySQL&style=social)](https://twitter.com/intent/follow?screen_name=MySQL)