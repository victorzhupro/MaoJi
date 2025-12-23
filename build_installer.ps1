# build_installer.ps1
$ErrorActionPreference = "Stop"

Write-Host "Cleaning old publish files..." -ForegroundColor Cyan
if (Test-Path "Publish") { Remove-Item "Publish" -Recurse -Force }
if (!(Test-Path "Installer")) { New-Item -ItemType Directory -Force -Path "Installer" | Out-Null }

$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$hasInno = Test-Path $isccPath

# --- 1. Full Version (Self-Contained) ---
Write-Host "`n=== Building Full Version (Self-Contained) ===" -ForegroundColor Cyan

$fullPublishDir = "Publish\Full"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $fullPublishDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Full version published." -ForegroundColor Green
    
    $zipFull = "Installer\MaoJi_Full.zip"
    if (Test-Path $zipFull) { Remove-Item $zipFull }
    Write-Host "Zipping Full version..."
    Compress-Archive -Path "$fullPublishDir\*" -DestinationPath $zipFull
    Write-Host "Created Full Zip: $zipFull" -ForegroundColor Green

    if ($hasInno) {
        Write-Host "Compiling Full Installer..." -ForegroundColor Cyan
        & $isccPath "/dSourceDir=$fullPublishDir" "/dOutputFileName=MaoJi_Full_Setup" "setup.iss"
        if ($LASTEXITCODE -eq 0) {
             Write-Host "Full Installer created." -ForegroundColor Green
        } else {
             Write-Host "Full Installer compilation failed." -ForegroundColor Red
        }
    }
} else {
    Write-Host "Full version publish failed." -ForegroundColor Red
    exit 1
}

# --- 2. Lite Version (Framework-Dependent) ---
Write-Host "`n=== Building Lite Version (Framework-Dependent) ===" -ForegroundColor Cyan

$litePublishDir = "Publish\Lite"
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o $litePublishDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Lite version published." -ForegroundColor Green
    
    $zipLite = "Installer\MaoJi_Lite.zip"
    if (Test-Path $zipLite) { Remove-Item $zipLite }
    Write-Host "Zipping Lite version..."
    Compress-Archive -Path "$litePublishDir\*" -DestinationPath $zipLite
    Write-Host "Created Lite Zip: $zipLite" -ForegroundColor Green

    if ($hasInno) {
        Write-Host "Compiling Lite Installer..." -ForegroundColor Cyan
        & $isccPath "/dSourceDir=$litePublishDir" "/dOutputFileName=MaoJi_Lite_Setup" "setup.iss"
        if ($LASTEXITCODE -eq 0) {
             Write-Host "Lite Installer created." -ForegroundColor Green
        } else {
             Write-Host "Lite Installer compilation failed." -ForegroundColor Red
        }
    }
} else {
    Write-Host "Lite version publish failed." -ForegroundColor Red
    exit 1
}

Write-Host "`nAll builds completed successfully." -ForegroundColor Cyan
