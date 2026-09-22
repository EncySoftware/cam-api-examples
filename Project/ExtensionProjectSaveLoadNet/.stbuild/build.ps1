[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArguments)

$ErrorActionPreference = 'Stop'
$buildProject = Join-Path $PSScriptRoot 'build\stbuild.csproj'
& dotnet build $buildProject --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet run --project $buildProject --no-build -- --root $PSScriptRoot @BuildArguments
exit $LASTEXITCODE
