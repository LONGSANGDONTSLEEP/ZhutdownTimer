$ErrorActionPreference = 'Stop'

$compilerCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'The .NET Framework C# compiler was not found. Install the .NET Framework 4.8 Developer Pack or Visual Studio Build Tools.'
}

$outputDirectory = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$outputFile = Join-Path $outputDirectory 'ZhutdownTimer.exe'
$sourceDirectory = Join-Path $PSScriptRoot 'src\ZhutdownTimer'

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32manifest:"$sourceDirectory\app.manifest" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll `
    /out:"$outputFile" `
    "$sourceDirectory\AssemblyInfo.cs" "$sourceDirectory\TimerLogic.cs" "$sourceDirectory\Program.cs"

if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Host "Build complete: $outputFile"
