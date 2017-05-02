#!/bin/bash

cd MySQL.Data
dotnet restore
dotnet build src/MySql.Data.csproj -c Release -f netstandard1.3

cd ../EntityFrameworkCore
dotnet restore
dotnet build src/MySql.Data.EntityFrameworkCore/MySql.Data.EntityFrameworkCore.csproj -c Release -f netstandard1.3
dotnet build src/MySql.Data.EntityFrameworkCore/MySql.Data.EntityFrameworkCore.csproj -c Release -f netstandard1.3


