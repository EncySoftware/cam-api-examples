@echo off
setlocal
call "%~dp0..\.stbuild\build.cmd" --Target Compile --Variant Debug --no-logo %*
exit /b %ERRORLEVEL%
