cd MySql.Data
dotnet restore
dotnet build -c Release

cd ..\MySql.Web
dotnet restore
dotnet build -c Release

cd ..\EntityFramework6
dotnet restore
dotnet build -c Release

cd ..\EntityFrameworkCore
dotnet restore
dotnet build -c Release

REM ================= build EMTrace plugin ===================================
cd ..\EMTrace
dotnet restore
dotnet build -c Release

REM ==================  build installer custom action and docs ==============================
cd ..
msbuild Installer\CustomAction\MySql.ConnectorInstaller.csproj /p:Configuration=Release
msbuild Documentation\help.shfbproj /p:Configuration=Release

REM ================== sign assemblies ========================================
sn.exe -Rca  Installer\CustomAction\bin\Release\MySql.ConnectorInstaller.CA.dll ConnectorNet
sn.exe -Rca  MySql.Data\src\bin\release\net452\MySql.Data.dll ConnectorNet
sn.exe -Rca  EntityFramework6\src\bin\release\net452\MySql.Data.Entity.EF6.dll ConnectorNet
sn.exe -Rca  EntityFrameworkCore\src\MySql.Data.EntityFrameworkCore\bin\release\net452\MySql.Data.EntityFrameworkCore.dll ConnectorNet
sn.exe -Rca  MySql.Web\src\bin\release\net452\MySql.Web.dll ConnectorNet
sn.exe -Rca  EMTrace\src\bin\Release\net452\MySql.MonitorPlugin.dll ConnectorNet

REM ================== Sign netstandard assemblies ============================
sn.exe -Rca  MySql.Data\src\bin\release\netcoreapp1.1\MySql.Data.dll ConnectorNet
sn.exe -Rca  EntityFrameworkCore\src\MySql.Data.EntityFrameworkCore\bin\release\netcoreapp1.1\MySql.Data.EntityFrameworkCore.dll ConnectorNet

dotnet pack MySql.Data/src/MySql.Data.csproj -c Release
dotnet pack MySql.Data/src/MySql.Web.csproj -c Release



