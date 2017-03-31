IF NOT "%1" == ""  SET MYSQL_PORT=%1

cd MySql.Data\tests
dotnet restore MySql.Data.Tests.csproj
copy certificates\*.* %MYSQL_DATADIR%\
dotnet test MySql.Data.Tests.csproj -f net452 -c Debug
dotnet test MySql.Data.Tests.csproj -f netcoreapp1.1 -c Debug
cd ..\..

