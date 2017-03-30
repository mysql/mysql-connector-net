#!/bin/bash

cd MySql.Data
dotnet restore
dotnet build -c Release

cd ../EntityFrameworkCore
dotnet restore
dotnet build -c Release

