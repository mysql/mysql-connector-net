#!/bin/bash

cd MySQL.Data
dotnet restore
dotnet build -c Release -f netcoreapp1.0

cd ../EntityFrameworkCore
dotnet restore
dotnet build -c Release -f netcoreapp1.0


