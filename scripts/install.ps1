[CmdletBinding()]
param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'PaperTrade.sln'
$frontendDirectory = Join-Path $repositoryRoot 'src/frontend/papertrade-web'
$frontendPackage = Join-Path $frontendDirectory 'package.json'
$frontendLockfile = Join-Path $frontendDirectory 'package-lock.json'

if (-not (Test-Path -LiteralPath $solution) -or
    -not (Test-Path -LiteralPath $frontendPackage)) {
    throw 'Cannot find the backend solution or frontend package.json.'
}

function Assert-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name was not found on PATH."
    }
}

function Invoke-Checked {
    param([scriptblock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE."
    }
}

function Find-Dotnet {
    $command = Get-Command 'dotnet.exe' -CommandType Application -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($path in @(
        (Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'),
        (Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet/dotnet.exe')
    )) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $null
}

if (-not $SkipBackend) {
    $dotnet = Find-Dotnet
    if (-not $dotnet) {
        $installDirectory = Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet'
        $installer = Join-Path $env:TEMP 'papertrade-dotnet-install.ps1'
        Write-Host 'Installing .NET 10 SDK...' -ForegroundColor Cyan
        try {
            Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
            Invoke-Checked {
                powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer `
                    -Channel '10.0' -InstallDir $installDirectory -NoPath
            }
        }
        finally {
            Remove-Item -LiteralPath $installer -ErrorAction SilentlyContinue
        }
        $dotnet = Find-Dotnet
        if (-not $dotnet) {
            throw '.NET installation finished but dotnet.exe could not be found.'
        }
    }

    Write-Host 'Installing backend dependencies...' -ForegroundColor Cyan
    Invoke-Checked { & $dotnet restore $solution }
}

if (-not $SkipFrontend) {
    Assert-Command 'npm.cmd'
    Write-Host 'Installing frontend dependencies...' -ForegroundColor Cyan
    Push-Location $frontendDirectory
    try {
        if (Test-Path -LiteralPath $frontendLockfile) {
            Invoke-Checked { npm.cmd ci }
        }
        else {
            Invoke-Checked { npm.cmd install }
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'Installation complete.' -ForegroundColor Green
