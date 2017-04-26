#!/bin/bash

if [ "$#" -eq 1 ]; then
    export MYSQL_PORT="$1"
fi

cd MySQL.Data/tests
dotnet restore MySql.Data.Tests.csproj
dotnet xunit -framework netcoreapp1.1
cd ../..
