rem @echo off
rem SET PROJECT_DIR=%cd%

rem @echo on
IF "%1" == "gpl" (
  REM =============  Creating Nuget packages ===================================
  dotnet pack MySql.Data/src/MySql.Data.csproj -c Release -o ..\..\NugetPkgs\Package
  dotnet pack MySql.Web/src/MySql.Web.csproj -c Release -o ..\..\NugetPkgs\Package
)

Nuget.exe install msbuildtasks -o packages
Nuget.exe install MSBuild.Extension.Pack -o packages
copy Internal\package.proj .

msbuild package.proj /p:Configuration=%1

IF %ERRORLEVEL% NEQ 0 EXIT /B 1

