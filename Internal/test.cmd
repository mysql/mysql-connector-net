IF NOT "%1" == ""  SET MYSQL_PORT=%1


REM =================== Test MySql.Data ==================================================
cd MySql.Data\tests
dotnet clean
dotnet restore 
copy certificates\*.* %MYSQL_DATADIR%\
dotnet build MySql.Data.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.Data.dll ConnectorNet
sn.exe -Rca  bin\debug\net452\MySql.Data.Tests.dll ConnectorNet
dotnet xunit -framework net452 -parallel none -xml mysql-data-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml mysql-data-core-test-results.xml
cd ../..

REM =================== Test MySql.Web ==================================================
cd MySql.Web\tests
dotnet clean
dotnet restore 
dotnet build MySql.Web.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.Web.dll ConnectorNet
sn.exe -Rca  bin\debug\net452\MySql.Web.Tests.dll ConnectorNet
dotnet xunit -framework net452 -parallel none -xml mysql-web-test-results.xml
cd ../..

REM =================== Test EF Core =====================================================
cd EntityFrameworkCore/tests/MySql.EntityFrameworkCore.Basic.Tests
dotnet clean
del /S bin /Q
del /S obj /Q
del /S ..\..\src\MySql.Data.EntityFrameworkCore\bin /Q
del /S ..\..\src\MySql.Data.EntityFrameworkCore\obj /Q
dotnet restore
dotnet build MySql.EntityFrameworkCore.Basic.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.EntityFrameworkCore.Basic.Tests.dll ConnectorNet
dotnet xunit -framework net452 -parallel none -xml mysql-efcore-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml mysql-efcore-core-test-results.xml

cd ../MySql.EntityFrameworkCore.Design.Tests/
dotnet clean
dotnet restore
dotnet build MySql.EntityFrameworkCore.Design.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.EntityFrameworkCore.Design.Tests.dll ConnectorNet
dotnet xunit -framework net452 -parallel none -xml mysql-efcoredesign-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml mysq-efcoredesign-core-test-results.xml

cd ../MySql.EntityFrameworkCore.Migrations.Tests
dotnet clean
dotnet restore
dotnet build MySql.EntityFrameworkCore.Migrations.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.EntityFrameworkCore.Migrations.Tests.dll ConnectorNet
dotnet xunit -framework net452 -parallel none -xml mysql-efcoremigrations-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml mysql-efcoremigrations-core-test-results.xml

cd ../../..

REM =================== Test EF 6 =====================================================
cd EntityFramework6/tests/MySql.EntityFramework6.Basic.Tests
dotnet clean
dotnet restore
dotnet build MySql.EntityFramework6.Basic.Tests.csproj -c Debug
sn.exe -Rca  bin\debug\net452\MySql.EntityFramework6.Basic.Tests.dll ConnectorNet
dotnet xunit -parallel none -xml mysql-ef6-test-results.xml




