@echo off
IF "%1" == "" ((echo Usage: runpip [testname] [Meta^|Product] [PipNNNNNNNN]) & exit /B 1)
IF "%2" == "" ((echo Usage: runpip [testname] [Meta^|Product] [PipNNNNNNNN]) & exit /B 1)
IF "%3" == "" ((echo Usage: runpip [testname] [Meta^|Product] [PipNNNNNNNN]) & exit /B 1)

call %~dp0pipbat %1 %2 %3
%~dp0intermediate\%1\%2BuildGraph\%3.bat
