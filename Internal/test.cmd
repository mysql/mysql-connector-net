IF NOT "%1" == ""  SET MYSQL_PORT=%1

cd MySql.Data\tests
dotnet restore MySql.Data.Tests.csproj
copy certificates\*.* %MYSQL_DATADIR%\
dotnet build MySql.Data.Tests.csproj -c Debug

REM ================== Register a verification exception ================================
sn.exe -Vr ../src/bin/debug/net452/MySql.Data.dll
sn.exe -Vr ../tests/bin/debug/net452/MySql.Data.Tests.dll

REM =================== Now test! =======================================================
dotnet test MySql.Data.Tests.csproj -f net452 -c Debug
dotnet test MySql.Data.Tests.csproj -f netcoreapp1.0 -c Debug

cd ..\..

