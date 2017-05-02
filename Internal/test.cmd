IF NOT "%1" == ""  SET MYSQL_PORT=%1

cd MySql.Data\tests
dotnet restore MySql.Data.Tests.csproj
copy certificates\*.* %MYSQL_DATADIR%\
dotnet build MySql.Data.Tests.csproj -c Debug

REM ================== Register a verification exception ================================
sn.exe -Rca  ..\src\bin\debug\net452\MySql.Data.dll ConnectorNet
sn.exe -Rca bin\debug\net452\MySql.Data.Tests.dll ConnectorNet

REM =================== Now test! =======================================================
dotnet xunit -framework net452 -parallel none -xml n452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml netcore-test-results.xml

cd ..\..

