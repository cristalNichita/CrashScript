param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$Root = (
    Resolve-Path (
        Join-Path $PSScriptRoot ".."
    )
).Path

$Project = Join-Path `
    $Root `
    "src\CrashScript.Cli\CrashScript.Cli.csproj"

$Output = Join-Path `
    $Root `
    "artifacts\$Runtime"

if (Test-Path $Output) {
    Remove-Item `
        $Output `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path $Output `
    -Force | Out-Null

Write-Host ""
Write-Host "Publishing CrashScript..."
Write-Host "Runtime: $Runtime"
Write-Host "Output:  $Output"
Write-Host ""

& dotnet publish `
    $Project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=embedded `
    --output $Output

if ($LASTEXITCODE -ne 0) {
    throw "CrashScript publishing failed with exit code $LASTEXITCODE."
}

$ExecutableName = if (
    $Runtime.StartsWith(
        "win-",
        [StringComparison]::OrdinalIgnoreCase
    )
) {
    "crashscript.exe"
}
else {
    "crashscript"
}

$ExecutablePath = Join-Path `
    $Output `
    $ExecutableName

if (-not (Test-Path $ExecutablePath)) {
    throw "Published executable was not found: $ExecutablePath"
}

$Executable = Get-Item $ExecutablePath

Write-Host ""
Write-Host "CrashScript published successfully."
Write-Host "Executable: $($Executable.FullName)"
Write-Host "Size:       $([Math]::Round($Executable.Length / 1MB, 2)) MB"
Write-Host ""