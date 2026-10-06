# Tek dosyalık exe üretir: publish\NTierGenerator.exe
#   .\publish.ps1                 .NET 10 Desktop Runtime gerektirir (.NET 10 SDK ile gelir), ~1 MB
#   .\publish.ps1 -SelfContained  .NET kurulu olmayan bilgisayarlar için, runtime exe'nin içinde
param([switch]$SelfContained)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$publishArguments = @(
    'publish', 'src/NTierGenerator.App/NTierGenerator.App.csproj',
    '-c', 'Release',
    '-r', 'win-x64',
    '-o', 'publish',
    '-p:PublishSingleFile=true',
    '-p:DebugType=none'
)

if ($SelfContained) {
    $publishArguments += '--self-contained', 'true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true'
} else {
    $publishArguments += '--self-contained', 'false'
}

dotnet @publishArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-Item publish\NTierGenerator.exe | Select-Object Name, @{ Name = 'Boyut (KB)'; Expression = { [math]::Round($_.Length / 1KB) } }
