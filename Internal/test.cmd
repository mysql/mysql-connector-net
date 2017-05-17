IF NOT "%1" == ""  SET MYSQL_PORT=%1


REM =================== Test MySql.Data ==================================================
cd MySql.Data\tests
dotnet restore 
copy certificates\*.* %MYSQL_DATADIR%\
dotnet xunit -framework net452 -parallel none -xml n452-test-results.xml
dotnet xunit -framework netcoreapp1.1 -parallel none -xml netcore-test-results.xml
cd ../..

REM =================== Test MySql.Web ==================================================
cd MySql.Web\tests
dotnet restore 
dotnet xunit -framework net452 -parallel none -xml n452-test-results.xml
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

REM =================== Test EF 6 =====================================================
cd EntityFramework6/tests/MySql.EntityFramework6.Basic.Tests
dotnet restore
dotnet xunit -xml net452-test-results.xml




