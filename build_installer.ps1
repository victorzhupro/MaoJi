# build_installer.ps1

Write-Host "Cleaning old publish files..." -ForegroundColor Cyan
if (Test-Path "Publish") {
    Remove-Item "Publish" -Recurse -Force
}

Write-Host "Publishing application (Self-Contained, Single File, Win-x64)..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true -o .\Publish

if ($LASTEXITCODE -eq 0) {
        Write-Host "Publish success! Files are in .\Publish directory." -ForegroundColor Green
        
        # Create Zip for lightweight deployment
        $zipFile = "Installer\MaoJi.zip"
        if (Test-Path $zipFile) { Remove-Item $zipFile }
        Compress-Archive -Path "Publish\*" -DestinationPath $zipFile
        Write-Host "Created lightweight zip deployment: $zipFile" -ForegroundColor Green

        $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $isccPath) {
        Write-Host "Inno Setup detected, compiling installer..." -ForegroundColor Cyan
        & $isccPath "setup.iss"
        if ($LASTEXITCODE -eq 0) {
             Write-Host "Installer created successfully! Check Installer directory." -ForegroundColor Green
        } else {
             Write-Host "Installer compilation failed." -ForegroundColor Red
        }
    } else {
        Write-Host "Inno Setup not found at default location. Please compile setup.iss manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "Publish failed. Check error messages." -ForegroundColor Red
}
