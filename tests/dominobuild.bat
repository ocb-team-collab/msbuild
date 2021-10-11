@echo off
IF "%1" == "" ((echo Please specify which test to run - e.g.: copy) & exit /B 1)

mkdir %~dp0artifacts\bin\MSBuild\Debug\net472\current\ 2> nul
copy %~dp0artifacts\bin\Microsoft.Build.Tasks\Debug\net472\Microsoft.Common.props %~dp0artifacts\bin\MSBuild\Debug\net472\current\Microsoft.common.props

IF NOT EXIST %~dp0..\DominoStaticMsBuild mkdir %~dp0..\DominoStaticMsBuild

set TargetProjFile=%~dp0tests\%1\project.proj
IF NOT EXIST %TargetProjFile% set TargetProjFile=%~dp0tests\%1\%1.csproj

echo Creating MetaBuild
%~dp0artifacts\bin\Microsoft.Build.TaskLauncher\Debug\net472\Microsoft.Build.TaskLauncher.exe meta %TargetProjFile% %~dp0..\DominoStaticMsBuild %~dp0artifacts\bin\MSBuild\Debug\net472\MSBuild.exe
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing MetaBuild
%PKGDOMINO%\bxl.exe /c:%~dp0..\DominoStaticMsBuild\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Creating ProductBuild
%~dp0artifacts\bin\Microsoft.Build.TaskLauncher\Debug\net472\Microsoft.Build.TaskLauncher.exe print %TargetProjFile%.graph.json %~dp0tests\%1
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing ProductBuild
%PKGDOMINO%\bxl.exe /c:%~dp0tests\%1\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%
