@echo off
IF "%1" == "" ((echo Usage: dominobuild [testname]) & exit /B 1)

set _Static_Artifacts_MsBuild=%~dp0..\artifacts\bin\MSBuild\Debug\net472
set _Static_Artifacts_MicrosftBuildTasks=%~dp0..\artifacts\bin\Microsoft.Build.Tasks\Debug\net472
set _Static_Artifacts_MicrosoftBuildTargetLauncher=%~dp0..\artifacts\bin\Microsoft.Build.TaskLauncher\Debug\net472
set _Static_Intermediate=%~dp0intermediate\%1
set _Static_Intermediate_MetaBuildGraph=%_Static_Intermediate%\MetaBuildGraph
set _Static_Intermediate_MetaBuildOutput=%_Static_Intermediate%\MetaBuildOutput
set _Static_Intermediate_ProductBuildGraph=%_Static_Intermediate%\ProductBuildGraph
set _Static_Intermediate_ProductBuildOutput=%_Static_Intermediate%\ProductBuildOutput
set _Static_Source=%~dp0%1

IF NOT EXIST %_Static_Artifacts_MsBuild%\current mkdir %_Static_Artifacts_MsBuild%\current
copy %_Static_Artifacts_MicrosftBuildTasks%\Microsoft.Common.props %_Static_Artifacts_MsBuild%\current\Microsoft.common.props

IF EXIST %_Static_Intermediate% rmdir /S/Q %_Static_Intermediate%
mkdir %_Static_Intermediate%
mkdir %_Static_Intermediate_MetaBuildGraph%
mkdir %_Static_Intermediate_MetaBuildOutput%
mkdir %_Static_Intermediate_ProductBuildGraph%
mkdir %_Static_Intermediate_ProductBuildOutput%

echo Creating MetaBuild
%_Static_Artifacts_MicrosoftBuildTargetLauncher%\Microsoft.Build.TaskLauncher.exe meta %_Static_Source% %_Static_Artifacts_MsBuild%\MSBuild.exe %_Static_Intermediate_MetaBuildGraph% %_Static_Intermediate_MetaBuildOutput% 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing MetaBuild
%PKGDOMINO%\bxl.exe /c:%_Static_Intermediate_MetaBuildGraph%\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Creating ProductBuild
%_Static_Artifacts_MicrosoftBuildTargetLauncher%\Microsoft.Build.TaskLauncher.exe print %_Static_Intermediate_MetaBuildOutput%\graph.json %_Static_Intermediate_ProductBuildGraph% %_Static_Intermediate_ProductBuildOutput%
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%

echo Executing ProductBuild
%PKGDOMINO%\bxl.exe /c:%_Static_Intermediate_ProductBuildGraph%\config.dsc 
IF ERRORLEVEL 1 exit /B %ERRORLEVEL%
