#!/bin/bash

cd MySQL.Data/tests
dotnet test MySql.Data.Tests.csproj -f netcoreapp1.1 -c Debug
cd ../..
