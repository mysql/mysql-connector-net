#!/bin/bash

cd MySQL.Data
dotnet restore
dotnet build -c Release -f netcoreapp1.1

cd ../EntityFrameworkCore
dotnet restore
dotnet build -c Release -f netcoreapp1.1


