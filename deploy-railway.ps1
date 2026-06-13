# Deploy meow na Railway z lokalnego folderu (bez GitHuba zespołu)
# Uruchom: cd d:\ccc\docs\meow-push ; Set-ExecutionPolicy -Scope Process Bypass ; .\deploy-railway.ps1

$ErrorActionPreference = "Stop"
$Railway = Join-Path $PSScriptRoot ".tools\railway.exe"

if (-not (Test-Path $Railway)) {
    Write-Host "Brak Railway CLI — pobieram..." -ForegroundColor Yellow
    $dir = Join-Path $PSScriptRoot ".tools"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $zip = Join-Path $dir "railway.zip"
    Invoke-WebRequest -Uri "https://github.com/railwayapp/cli/releases/download/v5.12.1/railway-v5.12.1-x86_64-pc-windows-msvc.zip" -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $dir -Force
}

Write-Host ""
Write-Host "=== KROK 1: Logowanie ===" -ForegroundColor Cyan
Write-Host "Otworzy sie przegladarka — zaloguj sie tym samym kontem co na railway.app"
Write-Host ""
& $Railway login

Write-Host ""
Write-Host "=== KROK 2: Nowy projekt ===" -ForegroundColor Cyan
& $Railway init --name meow

Write-Host ""
Write-Host "=== KROK 3: MySQL + Redis ===" -ForegroundColor Cyan
& $Railway add --database mysql
& $Railway add --database redis

Write-Host ""
Write-Host "=== KROK 4: Deploy aplikacji (Dockerfile) ===" -ForegroundColor Cyan
Write-Host "To moze potrwac kilka minut..."
Write-Host ""
& $Railway up -y --detach

Write-Host ""
Write-Host "=== KROK 5: Panel Railway ===" -ForegroundColor Cyan
& $Railway open

Write-Host ""
Write-Host "W panelu Railway (serwis aplikacji, NIE MySQL):" -ForegroundColor Green
Write-Host "1. Variables: ASPNETCORE_ENVIRONMENT, ASPNETCORE_URLS, Jwt__Key, ConnectionStrings"
Write-Host "2. MySQL + Redis connection stringi z Variables tych serwisow"
Write-Host "3. Networking -> Generate Domain (port 8080)"
Write-Host "4. Test: https://TWOJA-DOMENA.up.railway.app/swagger"
Write-Host "5. Register -> UPDATE Users SET Rola = 'Admin' WHERE Login = 'twoj_login';"
Write-Host "Logi: .\.tools\railway.exe logs" -ForegroundColor Green
