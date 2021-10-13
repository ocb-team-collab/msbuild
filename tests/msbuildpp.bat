@echo off
IF "%1" == "" ((echo Usage: dominobuild [testname]) & exit /B 1)

set _Static_Artifacts_MsBuild=%~dp0..\artifacts\bin\MSBuild\Debug\net472
set _Static_Source=%~dp0%1
set _Static_Intermediate=%~dp0intermediate\%1
set _Static_Intermediate_PreProcess=%~dp0intermediate\%1\PreProcess

IF NOT EXIST %_Static_Intermediate% mkdir %_Static_Intermediate%

IF EXIST %_Static_Intermediate_PreProcess% rmdir /S/Q %_Static_Intermediate_PreProcess%
mkdir %_Static_Intermediate_PreProcess%

%_Static_Artifacts_MsBuild%\MSBuild.exe %_Static_Source%\project.proj /pp:%_Static_Intermediate_PreProcess%\project.pp
echo %_Static_Intermediate_PreProcess%\project.pp
