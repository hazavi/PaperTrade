[CmdletBinding()]
param(
    [switch]$SkipInfrastructure
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot '.env'
$apiProject = Join-Path $repositoryRoot 'src/backend/PaperTrade.Api/PaperTrade.Api.csproj'
$frontendDirectory = Join-Path $repositoryRoot 'src/frontend/papertrade-web'
$frontendPackage = Join-Path $frontendDirectory 'package.json'
$dotnetCommand = Get-Command 'dotnet.exe' -CommandType Application -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCommand) { $dotnetCommand.Source } else { $null }
if (-not $dotnet) {
    foreach ($path in @(
        (Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'),
        (Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet/dotnet.exe')
    )) {
        if (Test-Path -LiteralPath $path) {
            $dotnet = $path
            break
        }
    }
}

if (-not $dotnet -or -not (Test-Path -LiteralPath $dotnet)) {
    throw 'The .NET SDK was not found. Run scripts/install.ps1 first.'
}

function Get-EnvironmentFileValues {
    param([string]$Path)

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) {
            continue
        }

        $separator = $trimmed.IndexOf('=')
        if ($separator -le 0) {
            continue
        }

        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim().Trim('"').Trim("'")
        $values[$name] = $value
    }

    return $values
}

function Stop-ChildProcess {
    param([System.Diagnostics.Process]$Process)

    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        $Process.WaitForExit(5000)
    }
}

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing $environmentFile. Copy .env.example to .env and set POSTGRES_PASSWORD first."
}

if (-not (Test-Path -LiteralPath $apiProject) -or
    -not (Test-Path -LiteralPath $frontendPackage)) {
    throw 'Run this script from the PaperTrade repository.'
}

$settings = Get-EnvironmentFileValues -Path $environmentFile
$requiredSettings = @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD')
foreach ($setting in $requiredSettings) {
    if (-not $settings.ContainsKey($setting) -or
        [string]::IsNullOrWhiteSpace($settings[$setting])) {
        throw "$setting is missing from .env."
    }
}

$docker = $null
if (-not $SkipInfrastructure) {
    $dockerCommand = Get-Command 'docker.exe' -CommandType Application -ErrorAction SilentlyContinue
    if ($dockerCommand) {
        $docker = $dockerCommand.Source
    }
    else {
        $dockerPaths = @(
            (Join-Path $env:LOCALAPPDATA 'Programs/DockerDesktop/resources/bin/docker.exe'),
            (Join-Path $env:ProgramFiles 'Docker/Docker/resources/bin/docker.exe')
        )
        foreach ($dockerPath in $dockerPaths) {
            if (Test-Path -LiteralPath $dockerPath) {
                $docker = $dockerPath
                break
            }
        }
    }

    if (-not $docker) {
        throw 'Docker CLI was not found. Install and start Docker Desktop, then rerun this script. If PostgreSQL and Redis already run locally, use -SkipInfrastructure.'
    }
}

$apiProcess = $null
$frontendProcess = $null
$previousPath = $env:PATH
$previousEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    ConnectionStrings__DefaultConnection = $env:ConnectionStrings__DefaultConnection
    Redis__ConnectionString = $env:Redis__ConnectionString
    MarketData__Finnhub__ApiKey = $env:MarketData__Finnhub__ApiKey
    MarketData__TwelveData__ApiKey = $env:MarketData__TwelveData__ApiKey
    MarketDataWorker__Enabled = $env:MarketDataWorker__Enabled
    Trading__FeeBps = $env:Trading__FeeBps
    Trading__SlippageBps = $env:Trading__SlippageBps
    Cors__AllowedOrigins__0 = $env:Cors__AllowedOrigins__0
    VITE_API_BASE_URL = $env:VITE_API_BASE_URL
}

try {
    Push-Location $repositoryRoot

    if (-not $SkipInfrastructure) {
        $env:PATH = "$(Split-Path -Parent $docker);$env:PATH"
        Write-Host 'Starting PostgreSQL and Redis...'
        $savedErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        & $docker compose stop papertrade-api papertrade-web 2>$null
        $stopExitCode = $LASTEXITCODE
        & $docker compose up --detach --wait postgres redis
        $infrastructureExitCode = $LASTEXITCODE
        $ErrorActionPreference = $savedErrorActionPreference

        if ($stopExitCode -ne 0) {
            throw 'Existing API and frontend containers could not be stopped.'
        }
        if ($infrastructureExitCode -ne 0) {
            throw 'Docker Compose could not start PostgreSQL and Redis. See the Docker error above.'
        }
    }

    if (-not (Test-Path -LiteralPath (Join-Path $frontendDirectory 'node_modules'))) {
        Write-Host 'Installing frontend dependencies...'
        npm --prefix $frontendDirectory install
        if ($LASTEXITCODE -ne 0) {
            throw 'Frontend dependency installation failed.'
        }
    }

    $postgresPort = if ($settings.ContainsKey('POSTGRES_PORT')) { $settings.POSTGRES_PORT } else { '5432' }
    $redisPort = if ($settings.ContainsKey('REDIS_PORT')) { $settings.REDIS_PORT } else { '6379' }
    $apiPort = if ($settings.ContainsKey('API_PORT')) { $settings.API_PORT } else { '5044' }
    $webPort = if ($settings.ContainsKey('WEB_PORT')) { $settings.WEB_PORT } else { '5173' }

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "http://localhost:$apiPort"
    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=$postgresPort;Database=$($settings.POSTGRES_DB);Username=$($settings.POSTGRES_USER);Password=$($settings.POSTGRES_PASSWORD)"
    $env:Redis__ConnectionString = "localhost:$redisPort"
    $env:MarketData__Finnhub__ApiKey = if ($settings.ContainsKey('FINNHUB_API_KEY')) { $settings.FINNHUB_API_KEY } else { '' }
    $env:MarketData__TwelveData__ApiKey = if ($settings.ContainsKey('TWELVE_DATA_API_KEY')) { $settings.TWELVE_DATA_API_KEY } else { '' }
    $env:MarketDataWorker__Enabled = if ($settings.ContainsKey('MARKET_DATA_WORKER_ENABLED')) { $settings.MARKET_DATA_WORKER_ENABLED } else { 'true' }
    $env:Trading__FeeBps = if ($settings.ContainsKey('TRADING_FEE_BPS')) { $settings.TRADING_FEE_BPS } else { '1' }
    $env:Trading__SlippageBps = if ($settings.ContainsKey('TRADING_SLIPPAGE_BPS')) { $settings.TRADING_SLIPPAGE_BPS } else { '0' }
    $env:Cors__AllowedOrigins__0 = "http://localhost:$webPort"
    $env:VITE_API_BASE_URL = "http://localhost:$apiPort"

    Write-Host "Starting API at http://localhost:$apiPort ..."
    $apiProcess = Start-Process $dotnet `
        -ArgumentList @('watch', '--project', $apiProject, 'run', '--no-launch-profile') `
        -WorkingDirectory $repositoryRoot `
        -NoNewWindow `
        -PassThru

    Write-Host "Starting frontend at http://localhost:$webPort ..."
    $frontendProcess = Start-Process npm.cmd `
        -ArgumentList @('run', 'dev', '--', '--port', $webPort) `
        -WorkingDirectory $frontendDirectory `
        -NoNewWindow `
        -PassThru

    Write-Host 'PaperTrade is starting. Press Ctrl+C to stop the API and frontend.'

    while (-not $apiProcess.HasExited -and -not $frontendProcess.HasExited) {
        Start-Sleep -Milliseconds 500
    }

    if ($apiProcess.HasExited) {
        throw "The API exited with code $($apiProcess.ExitCode)."
    }

    throw "The frontend exited with code $($frontendProcess.ExitCode)."
}
finally {
    if ($apiProcess -or $frontendProcess) {
        Write-Host 'Stopping PaperTrade development processes...'
    }
    Stop-ChildProcess -Process $frontendProcess
    Stop-ChildProcess -Process $apiProcess
    $env:PATH = $previousPath

    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item "Env:$($entry.Key)" $entry.Value
        }
    }

    Pop-Location -ErrorAction SilentlyContinue
}
