rem @echo off
rem SET PROJECT_DIR=%cd%

rem @echo on
IF "%1" == "gpl" (
  REM =============  Creating Nuget packages ===================================
  dotnet restore MySql.Data/src/MySql.Data.csproj
  dotnet pack MySql.Data/src/MySql.Data.csproj -c Release -o ..\..\NugetPkgs\Package

  dotnet restore MySql.Web/src/MySql.Web.csproj
  dotnet pack MySql.Web/src/MySql.Web.csproj -c Release -o ..\..\NugetPkgs\Package

  dotnet restore EntityFrameworkCore/src/MySql.Data.EntityFrameworkCore/MySql.Data.EntityFrameworkCore.csproj
  dotnet pack EntityFrameworkCore/src/MySql.Data.EntityFrameworkCore/MySql.Data.EntityFrameworkCore.csproj -c Release -o ..\..\..\NugetPkgs\Package

  dotnet restore EntityFrameworkCore/src/MySql.Data.EntityFrameworkCore.Design/MySql.Data.EntityFrameworkCore.Design.csproj
  dotnet pack EntityFrameworkCore/src/MySql.Data.EntityFrameworkCore.Design/MySql.Data.EntityFrameworkCore.Design.csproj -c Release -o ..\..\..\NugetPkgs\Package

  dotnet restore EntityFramework6/src/MySql.Data.Entity.EF6.csproj
  dotnet pack EntityFramework6/src/MySql.Data.Entity.EF6.csproj -c Release -o ..\..\NugetPkgs\Package
)

Nuget.exe install msbuildtasks -o packages
Nuget.exe install MSBuild.Extension.Pack -o packages
copy Internal\package.proj .

msbuild package.proj /p:Configuration=%1

IF %ERRORLEVEL% NEQ 0 EXIT /B 1

