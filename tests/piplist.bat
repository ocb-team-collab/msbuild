@echo off
IF "%1" == "" ((echo Usage: getpips [testname] [Meta^|Product]) & exit /B 1)
IF "%2" == "" ((echo Usage: getpips [testname] [Meta^|Product]) & exit /B 1)

%PKGDOMINO%\bxlanalyzer.exe /m:DumpProcess /xl:%~dp0intermediate\%1\%2BuildGraph\Out\Logs\BuildXL.xlg /o:%~dp0intermediate\%1\%2BuildGraph\ProcessDump.xml
echo %~dp0intermediate\%1\%2BuildGraph\ProcessDump.xml

call p >NUL 2>NUL
if NOT ERRORLEVEL 1 p "o jn x { $i->{'Description'}[0] } x { a k 'Process', $i } a k 'SpecFile', tss a k 'ProcessDump', xd r $A0" %~dp0intermediate\%1\%2BuildGraph\ProcessDump.xml