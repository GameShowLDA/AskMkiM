param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = Join-Path $PSScriptRoot 'DockingFreezeRepro.csproj'
$assembly = Join-Path $repository 'Bin/DockingFreezeRepro/DockingFreezeRepro.dll'
$results = Join-Path ([IO.Path]::GetTempPath()) ('AskMkiM.FreezeVerification/' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($results) | Out-Null

if (-not $SkipBuild) {
  & dotnet build $project --nologo -v:q *> (Join-Path $results 'build.log')
  if ($LASTEXITCODE -ne 0) { throw "Build failed: $results/build.log" }
}

$scenarios = @(
  @{ Mode = 'detach'; Exit = 0; Signature = 'BeginDrag exception:' },
  @{ Mode = 'collapse'; Exit = 0; Signature = 'BeginDrag returned without exception' },
  @{ Mode = 'close-runner'; Exit = 0; Signature = 'BeginDrag exception:' },
  @{ Mode = 'blocking'; Exit = 2; Signature = 'WATCHDOG:'; Continuation = 'JsonFileWriter+<WriteAsync>' },
  @{ Mode = 'blocking-worker'; Exit = 0; Signature = 'Crash collector failed: Screenshot:' },
  @{ Mode = 'async'; Exit = 0; Signature = 'Crash package created:' }
)

foreach ($scenario in $scenarios) {
  $log = Join-Path $results ($scenario.Mode + '.log')
  & dotnet $assembly $scenario.Mode *> $log
  $actualExit = $LASTEXITCODE
  $content = Get-Content -LiteralPath $log -Raw
  if ($actualExit -ne $scenario.Exit -or -not $content.Contains($scenario.Signature)) {
    throw "Scenario $($scenario.Mode) failed: exit=$actualExit; log=$log"
  }
  if ($scenario.Continuation -and -not $content.Contains($scenario.Continuation)) {
    throw "Expected JsonFileWriter continuation was not observed: $log"
  }
  Write-Output "PASS $($scenario.Mode) (expected exit $actualExit)"
}
Write-Output "Evidence: $results"
