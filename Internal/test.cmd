IF NOT "%1" == ""  SET MYSQL_PORT=%1

cd MySql.Data\tests
dotnet restore 
copy certificates\*.* %MYSQL_DATADIR%\

REM =================== Test MySql.Data ==================================================
dotnet xunit -framework net452 -parallel none -xml n452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml netcore-test-results.xml
cd ../..

REM =================== Test EF Core =====================================================
cd EntityFrameworkCore/tests/MySql.EntityFrameworkCore.Basic.Tests
dotnet restore
dotnet xunit -framework net452 -xml net452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -xml netcore-test-results.xml

cd ../MySql.EntityFrameworkCore.Design.Tests/
dotnet restore
dotnet xunit -framework net452 -xml net452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -xml netcore-test-results.xml

cd ../MySql.EntityFrameworkCore.Migrations.Tests
dotnet restore
dotnet xunit -framework net452 -xml net452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -xml netcore-test-results.xml

cd ../../..



