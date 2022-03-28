@echo off
IF "%1" == "" ((echo Usage: dominobuild [testname]) & exit /B 1)

set _Static_Artifacts_MsBuild=%~dp0..\artifacts\bin\MSBuild\Debug\net472
set _Static_Source=%~dp0%1

%_Static_Artifacts_MsBuild%\MSBuild.exe %MSBUILDVERBOSITY% %_Static_Source%\project.proj
