cd MySql.Data\tests
copy certificates\*.* %MYSQL_DATADIR%\
dotnet test MySql.Data.Tests.csproj -f net452 -c Debug
dotnet test MySql.Data.Tests.csproj -f netcoreapp1.1 -c Debug
cd ..\..

