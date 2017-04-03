rem @echo off
rem SET PROJECT_DIR=%cd%

rem @echo on
Nuget.exe install msbuildtasks -o packages
Nuget.exe install MSBuild.Extension.Pack -o packages
copy Internal\package.proj .

msbuild package.proj /p:Configuration=%1

IF %ERRORLEVEL% NEQ 0 EXIT /B 1

