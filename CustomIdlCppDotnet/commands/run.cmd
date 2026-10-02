@echo off
setlocal
pushd "%~dp0.." || exit /b 1
dotnet run --project ".stbuild\build\stbuild.csproj" -- --root .stbuild --target Run --variant Release %*
set "run_result=%errorlevel%"
popd
exit /b %run_result%
