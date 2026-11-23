<#
.SYNOPSIS
Builds Invy for Windows: the installer (Invy-<version>-win-x64.msi) and the portable app
(Invy-<version>-win-x64-portable.exe), both with PostgreSQL inside.

.DESCRIPTION
Run it from the repository root after building the web page (./dev.sh web, or npm run build in src/Invy.Web)
and the documentation (mkdocs build into site/), as .github/workflows/release.yml does. The steps:
  1. download the PostgreSQL pinned in postgres.json, check its checksum and keep only what the server needs;
  2. write Wolverine's generated code, as the Dockerfile does;
  3. publish Invy for win-x64 twice: as a folder for the installer, and as one self-extracting exe;
  4. build the MSI with WiX (Windows only; -SkipInstaller leaves it out, for example on Linux).

.EXAMPLE
pwsh deploy/windows/build.ps1 -Version 1.4.0
#>
param(
    [string] $Version = "0.0.0",
    [string] $Output = "artifacts/windows",
    [string] $WebDist = "src/Invy.Web/dist",
    [string] $HelpSite = "site",
    [switch] $SkipInstaller
)

$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $here "../..")).Path
Set-Location $repo

function Step([string] $message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Run([string] $file, [string[]] $arguments) {
    & $file @arguments
    if ($LASTEXITCODE -ne 0) { throw "$file $($arguments -join ' ') failed with exit code $LASTEXITCODE." }
}

if (-not (Test-Path (Join-Path $WebDist "index.html"))) {
    throw "The web page isn't built in $WebDist. Run ./dev.sh web (or npm run build in src/Invy.Web) first."
}

$work = Join-Path $Output "work"
$payload = Join-Path $work "payload"
Remove-Item -Recurse -Force $payload -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $payload, (Join-Path $work "cache") | Out-Null

# 1. PostgreSQL. EDB's zip also holds pgAdmin, StackBuilder, headers and import libraries; the server needs bin, lib
# (minus the .lib and .a files) and share (minus translations). lib and share/extension keep ltree and pg_trgm, which
# Invy's Baseline migration turns on; the check after the loop fails the build without them. Its programs need the Visual C++ runtime, which
# Windows doesn't always have; the zip's own copy (under pgAdmin's Python) goes next to them.
$postgres = Get-Content (Join-Path $here "postgres.json") | ConvertFrom-Json
$zip = Join-Path $work "cache/postgresql-$($postgres.version).zip"
if (-not (Test-Path $zip)) {
    Step "Downloading PostgreSQL $($postgres.version)"
    Invoke-WebRequest -Uri $postgres.url -OutFile "$zip.partial"
    Move-Item -Force "$zip.partial" $zip
}
$hash = (Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
if ($hash -ne $postgres.sha256) {
    Remove-Item $zip
    throw "The PostgreSQL download's checksum is $hash, not $($postgres.sha256) as postgres.json says. It was deleted; run again, and if it still differs, check postgres.json."
}

Step "Unpacking the PostgreSQL server"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$pgsql = Join-Path $payload "pgsql"
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zip).Path)
try {
    foreach ($entry in $archive.Entries) {
        $name = $entry.FullName -replace '\\', '/'
        if ($name.EndsWith("/")) { continue }
        $keep = $false
        if ($name -match '^pgsql/(bin|lib|share)/') { $keep = $true }
        if ($name -match '^pgsql/bin/(wx|stackbuilder)' -or $name -match '^pgsql/(bin|lib)/.*\.(lib|a)$' -or $name -match '^pgsql/share/locale/') { $keep = $false }
        $target = $null
        if ($keep) { $target = Join-Path $payload $name }
        elseif ($name -match '^pgsql/pgAdmin 4/python/(vcruntime140(_1)?\.dll)$') { $target = Join-Path $pgsql "bin/$($Matches[1])" }
        elseif ($name -match '^pgsql/(server_license|commandlinetools_3rd_party_licenses)\.txt$') { $target = Join-Path $payload $name }
        if ($null -eq $target) { continue }
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
    }
} finally {
    $archive.Dispose()
}
foreach ($required in "bin/postgres.exe", "bin/pg_ctl.exe", "bin/initdb.exe", "bin/vcruntime140.dll", "lib/plpgsql.dll",
        "lib/ltree.dll", "lib/pg_trgm.dll", "share/extension/ltree.control", "share/extension/pg_trgm.control") {
    if (-not (Test-Path (Join-Path $pgsql $required))) { throw "The PostgreSQL zip has no pgsql/$required; its layout has changed." }
}

# The page, the help pages and the Windows defaults (HTTPS on, as in the container image).
Step "Gathering the web page and help"
Copy-Item -Recurse $WebDist (Join-Path $payload "wwwroot")
if (Test-Path (Join-Path $HelpSite "index.html")) {
    Copy-Item -Recurse $HelpSite (Join-Path $payload "wwwroot/help")
} else {
    Write-Warning "The documentation isn't built in $HelpSite, so /help will say so. Run mkdocs build first to include it."
}
Copy-Item (Join-Path $here "appsettings.Production.json") $payload
$payloadPath = (Resolve-Path $payload).Path

# 2. The Release build compiles the code Wolverine generates, as in the Dockerfile.
Step "Writing Wolverine's generated code"
Remove-Item -Recurse -Force "src/Invy.Api/Internal/Generated" -ErrorAction SilentlyContinue
Run dotnet @("run", "--project", "src/Invy.Api", "--", "codegen", "write")

# 3. Publish. The exe is renamed Invy.exe; its launcher finds Invy.Api.dll by the name built into it.
$common = @("publish", "src/Invy.Api/Invy.Api.csproj", "--configuration", "Release", "--runtime", "win-x64",
    "--self-contained", "-p:Version=$Version", "-p:InvyPayload=$payloadPath")

Step "Publishing Invy for the installer"
$app = Join-Path $work "app"
Remove-Item -Recurse -Force $app -ErrorAction SilentlyContinue
Run dotnet ($common + @("--output", $app))
Move-Item -Force (Join-Path $app "Invy.Api.exe") (Join-Path $app "Invy.exe")

Step "Publishing the portable Invy.exe"
$portable = Join-Path $work "portable"
Remove-Item -Recurse -Force $portable -ErrorAction SilentlyContinue
Run dotnet ($common + @("--output", $portable, "-p:PublishSingleFile=true", "-p:IncludeAllContentForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true", "-p:InvyPortable=true", "-p:DebugType=none"))
$portableExe = Join-Path $Output "Invy-$Version-win-x64-portable.exe"
Copy-Item -Force (Join-Path $portable "Invy.Api.exe") $portableExe

# 4. The installer.
if ($SkipInstaller) {
    Write-Host "Skipped the installer (-SkipInstaller)."
} elseif (-not $IsWindows) {
    Write-Warning "The installer can only be built on Windows; skipped it."
} else {
    Step "Building the installer"
    # MSI versions are three numbers; a pre-release suffix (1.4.0-rc.1) is dropped.
    $msiVersion = ($Version -split '-')[0]
    Run dotnet @("build", (Join-Path $here "Invy.Installer.wixproj"), "--configuration", "Release",
        "-p:Version=$msiVersion", "-p:AppFolder=$((Resolve-Path $app).Path)", "--output", $work)
    Copy-Item -Force (Join-Path $work "Invy.msi") (Join-Path $Output "Invy-$Version-win-x64.msi")
}

Step "Done"
Get-ChildItem $Output -File | Format-Table Name, @{ Label = "MB"; Expression = { [math]::Round($_.Length / 1MB, 1) } }
