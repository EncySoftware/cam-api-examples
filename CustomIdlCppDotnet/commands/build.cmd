@echo off
setlocal
pushd "%~dp0.." || exit /b 1
dotnet run --project ".stbuild\build\stbuild.csproj" -- --root .stbuild --target Compile --variant Release %*
set "build_result=%errorlevel%"
popd
exit /b %build_result%
