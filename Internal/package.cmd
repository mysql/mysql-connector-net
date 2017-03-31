rem @echo off
rem SET PROJECT_DIR=%cd%

rem @echo on
Nuget.exe install msbuildtasks -o packages
Nuget.exe install MSBuild.Extension.Pack -o packages
copy Internal\package.proj .

msbuild package.proj /p:Configuration=%1

IF "%1" == "commercial" EXIT 0

REM ================== Make Nuget packages ======================================
dotnet pack MySql.Data/src/MySql.Data.csproj -c Release
dotnet pack MySql.Web/src/MySql.Web.csproj -c Release