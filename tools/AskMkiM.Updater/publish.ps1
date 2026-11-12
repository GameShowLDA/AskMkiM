param(
  [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$toolDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $toolDirectory)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
  $OutputDirectory = Join-Path $repositoryRoot 'artifacts\AskMkiM.Updater.Authoring'
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$buildDirectory = Join-Path $resolvedOutput '.build'
$builderDirectory = Join-Path $buildDirectory 'builder'
$stubDirectory = Join-Path $buildDirectory 'stub'

New-Item -ItemType Directory -Path $builderDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $stubDirectory -Force | Out-Null

dotnet publish (Join-Path $repositoryRoot 'tools\AskMkiM.Updater.Builder\AskMkiM.Updater.Builder.csproj') `
  -c Release -o $builderDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish (Join-Path $repositoryRoot 'tools\AskMkiM.Updater.Stub\AskMkiM.Updater.Stub.csproj') `
  -c Release -o $stubDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -LiteralPath (Join-Path $builderDirectory 'AskMkiM.UpdateBuilder.exe') `
  -Destination (Join-Path $resolvedOutput 'AskMkiM.UpdateBuilder.exe') -Force
Copy-Item -LiteralPath (Join-Path $stubDirectory 'AskMkiM.Updater.Stub.exe') `
  -Destination (Join-Path $resolvedOutput 'AskMkiM.Updater.Stub.exe') -Force

$validatedBuildDirectory = [System.IO.Path]::GetFullPath($buildDirectory)
if ($validatedBuildDirectory.StartsWith($resolvedOutput + [System.IO.Path]::DirectorySeparatorChar)) {
  Remove-Item -LiteralPath $validatedBuildDirectory -Recurse -Force
}

Write-Host "Конструктор опубликован в $resolvedOutput"
