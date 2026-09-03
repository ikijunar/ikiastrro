#Requires -RunAsAdministrator
<#
.SYNOPSIS
    One-shot setup to host Ikiastrro.Web (ASP.NET Core 8 Blazor Server) in full IIS.

.DESCRIPTION
    Idempotent. Safe to re-run. Performs:
      1. dotnet publish of src\Ikiastrro.Web  -> <repo>\publish\web
      2. Installs the ASP.NET Core 8.0 Hosting Bundle if IIS lacks AspNetCoreModuleV2
      3. Creates IIS app pool 'ikiastrro' (No Managed Code) + site on the chosen port
      4. Grants the app-pool identity filesystem + SQL Server access
         (connection string: Server=localhost;Database=ikiastrro;Integrated Security=True)
      5. Starts the site and runs an HTTP smoke test

.NOTES
    Run from an ELEVATED PowerShell:  .\scripts\iis-setup.ps1
#>
[CmdletBinding()]
param(
    [string]$SiteName     = 'ikiastrro',
    [string]$AppPoolName   = 'ikiastrro',
    [int]   $Port          = 8080,
    [string]$RepoRoot      = (Split-Path -Parent $PSScriptRoot),
    [string]$SqlInstance   = 'localhost',
    [string]$Database      = 'ikiastrro',
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$PublishDir = Join-Path $RepoRoot 'publish\web'
$WebProj    = Join-Path $RepoRoot 'src\Ikiastrro.Web\Ikiastrro.Web.csproj'
$AppPoolSid = "IIS APPPOOL\$AppPoolName"

function Info($m) { Write-Host "[ikiastrro-iis] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "[ikiastrro-iis] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "[ikiastrro-iis] $m" -ForegroundColor Yellow }

# ---------------------------------------------------------------------------
# 1. Publish
# ---------------------------------------------------------------------------
if (-not $SkipPublish) {
    Info "Publishing $WebProj -> $PublishDir"
    dotnet publish $WebProj -c Release -o $PublishDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
} else {
    Info "SkipPublish set - using existing $PublishDir"
}
if (-not (Test-Path (Join-Path $PublishDir 'Ikiastrro.Web.dll'))) {
    throw "Ikiastrro.Web.dll not found in $PublishDir - publish first (drop -SkipPublish)."
}
New-Item -ItemType Directory -Force -Path (Join-Path $PublishDir 'logs') | Out-Null

# ---------------------------------------------------------------------------
# 2. ASP.NET Core Hosting Bundle (full-IIS ANCM v2)
# ---------------------------------------------------------------------------
$ancm = Join-Path $env:windir 'System32\inetsrv\aspnetcorev2.dll'
if (Test-Path $ancm) {
    Ok "AspNetCoreModuleV2 already present."
} else {
    Info "Hosting Bundle missing. Downloading ASP.NET Core 8.0 Hosting Bundle..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $exe = Join-Path $env:TEMP 'dotnet-hosting-8.0-win.exe'
    Invoke-WebRequest -Uri 'https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe' -OutFile $exe -UseBasicParsing
    Info "Installing $exe (quiet)..."
    $p = Start-Process -FilePath $exe -ArgumentList '/install','/quiet','/norestart' -Wait -PassThru
    if ($p.ExitCode -notin 0,3010) { throw "Hosting Bundle installer exit code $($p.ExitCode)" }
    Ok "Hosting Bundle installed. Restarting IIS..."
    & "$env:windir\System32\net.exe" stop was /y | Out-Null
    & "$env:windir\System32\net.exe" start w3svc | Out-Null
    if (-not (Test-Path $ancm)) { throw "aspnetcorev2.dll still missing after install - reboot may be required." }
}

Import-Module WebAdministration

# ---------------------------------------------------------------------------
# 3. App pool + site
# ---------------------------------------------------------------------------
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    Info "Creating app pool '$AppPoolName'"
    New-WebAppPool -Name $AppPoolName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''      # No Managed Code
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode             -Value 'AlwaysRunning'
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value 'ApplicationPoolIdentity'
Ok "App pool '$AppPoolName' configured (No Managed Code)."

$portInUse = Get-NetTCPListener -LocalPort $Port -ErrorAction SilentlyContinue
if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    if ($portInUse) { throw "Port $Port is already in use by PID $($portInUse.OwningProcess). Pick another -Port." }
    Info "Creating site '$SiteName' on port $Port -> $PublishDir"
    New-WebSite -Name $SiteName -PhysicalPath $PublishDir -Port $Port -ApplicationPool $AppPoolName -Force | Out-Null
} else {
    Info "Site '$SiteName' exists - updating path / binding / pool"
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath      -Value $PublishDir
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool   -Value $AppPoolName
    if (-not (Get-WebBinding -Name $SiteName -Port $Port -Protocol http -ErrorAction SilentlyContinue)) {
        if ($portInUse) { throw "Port $Port is already in use by PID $($portInUse.OwningProcess). Pick another -Port." }
        New-WebBinding -Name $SiteName -Protocol http -Port $Port -IPAddress '*'
    }
}
Ok "Site '$SiteName' bound to http://localhost:$Port"

# ---------------------------------------------------------------------------
# 4a. Filesystem ACLs for the app-pool identity
# ---------------------------------------------------------------------------
Info "Granting '$AppPoolSid' filesystem rights on $PublishDir"
& icacls.exe $PublishDir /grant "$($AppPoolSid):(OI)(CI)RX" /T /C /Q | Out-Null
& icacls.exe (Join-Path $PublishDir 'logs') /grant "$($AppPoolSid):(OI)(CI)M" /T /C /Q | Out-Null
Ok "Filesystem ACLs set."

# ---------------------------------------------------------------------------
# 4b. SQL Server login + db access for the app-pool identity
# ---------------------------------------------------------------------------
$sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
$tsql = @"
IF DB_ID(N'$Database') IS NULL
    RAISERROR('Database [$Database] does not exist on $SqlInstance. Restore/create it first (db\ikiastrro.sql).',16,1);
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$AppPoolSid')
    CREATE LOGIN [$AppPoolSid] FROM WINDOWS;
USE [$Database];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$AppPoolSid')
    CREATE USER [$AppPoolSid] FOR LOGIN [$AppPoolSid];
ALTER ROLE db_datareader ADD MEMBER [$AppPoolSid];
ALTER ROLE db_datawriter ADD MEMBER [$AppPoolSid];
GRANT EXECUTE TO [$AppPoolSid];
"@
if ($sqlcmd) {
    Info "Configuring SQL access for '$AppPoolSid' on [$Database]"
    $tmp = Join-Path $env:TEMP 'ikiastrro-sql-grant.sql'
    $tsql | Set-Content -Path $tmp -Encoding ASCII
    & $sqlcmd.Path -S $SqlInstance -E -b -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed ($LASTEXITCODE) - see message above." }
    Remove-Item $tmp -Force
    Ok "SQL access granted (db_datareader + db_datawriter + EXECUTE)."
} else {
    Warn "sqlcmd.exe not found. Run this T-SQL manually against $SqlInstance as an admin:"
    Write-Host $tsql -ForegroundColor DarkYellow
}

# ---------------------------------------------------------------------------
# 5. Start + smoke test
# ---------------------------------------------------------------------------
Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-WebSite    -Name $SiteName    -ErrorAction SilentlyContinue

$url = "http://localhost:$Port/"
Info "Smoke testing $url"
$ok = $false
foreach ($i in 1..10) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
        Ok "HTTP $($r.StatusCode) from $url  (length $($r.Content.Length))"
        $ok = $true; break
    } catch {
        Start-Sleep -Seconds 2
    }
}
if (-not $ok) {
    Warn "No successful response after retries. Check stdout log:"
    Warn "  $PublishDir\logs\stdout_*.log"
    Warn "Enable it by setting stdoutLogEnabled=`"true`" in $PublishDir\web.config, then re-hit the site."
    Get-ChildItem (Join-Path $PublishDir 'logs') -Filter 'stdout*' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Desc | Select-Object -First 1 | Get-Content -Tail 40 -ErrorAction SilentlyContinue
    exit 1
}

Ok "Done. Ikiastrro is running in IIS at $url"
