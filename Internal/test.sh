#!/bin/bash

if [ "$#" -eq 1 ]; then
    export MYSQL_PORT="$1"
fi

cd MySQL.Data/tests
dotnet restore MySql.Data.Tests.csproj
dotnet test MySql.Data.Tests.csproj -f netcoreapp1.1 -c Debug
cd ../..
