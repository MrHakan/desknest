$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $projectRoot "publish"

dotnet restore (Join-Path $projectRoot "DeskNest.sln")
dotnet build (Join-Path $projectRoot "DeskNest.sln") -c Release --no-restore
dotnet publish (Join-Path $projectRoot "src/DeskNest/DeskNest.csproj") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $output

Write-Host "DeskNest published to $output"
