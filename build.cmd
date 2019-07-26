@echo off
set DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR=%~dp0.dotnet\sdk\3.0.100-preview6-012264\Sdks
powershell -NoLogo -NoProfile -ExecutionPolicy ByPass -Command "& """%~dp0eng\common\build.ps1""" -build -restore %*"
exit /b %ErrorLevel%
