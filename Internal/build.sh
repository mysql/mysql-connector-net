#!/bin/bash

cd MySQL.Data/src
dotnet restore
dotnet build -c Release -f netstandard1.3

cd ../EntityFrameworkCore/src
dotnet restore
dotnet build -c Release -f netstandard1.3


