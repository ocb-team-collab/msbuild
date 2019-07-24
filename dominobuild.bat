@echo off
IF "%1" == "" ((echo Please specify which test to run - e.g.: copy) & exit /B 1)

IF NOT EXIST %~dp0..\DominoStaticMsBuild mkdir %~dp0..\DominoStaticMsBuild

echo Creating MetaBuild
%~dp0artifacts\bin\Microsoft.Build.TaskLauncher\Debug\net472\Microsoft.Build.TaskLauncher.exe meta %~dp0tests\%1\project.proj %~dp0..\DominoStaticMsBuild %~dp0artifacts\bin\MSBuild\Debug\net472\MSBuild.exe
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing MetaBuild
%PKGDOMINO%\bxl.exe /c:%~dp0..\DominoStaticMsBuild\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Creating ProductBuild
%~dp0artifacts\bin\Microsoft.Build.TaskLauncher\Debug\net472\Microsoft.Build.TaskLauncher.exe print %~dp0tests\%1\project.proj.graph.json %~dp0tests\%1
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing ProductBuild
%PKGDOMINO%\bxl.exe /c:tests\copy\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%
