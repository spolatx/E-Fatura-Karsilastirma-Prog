# ==============================================================================
# Script: publish-for-windows.ps1
# Description: Builds the Next.js static export, copies it to API wwwroot,
#              cleans target output, and publishes the .NET 10 Web API as a
#              single self-contained Win-x64 executable for Windows Service / SCM.
# ==============================================================================

$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
$FrontendDir = Join-Path $RootDir "frontend"
$FrontendOutDir = Join-Path $FrontendDir "out"
$ApiProjectDir = Join-Path $RootDir "src\Presentation\InvoiceComparison.Api"
$ApiProjectPath = Join-Path $ApiProjectDir "InvoiceComparison.Api.csproj"
$WwwrootDir = Join-Path $ApiProjectDir "wwwroot"
$PublishOutputDir = "C:\EksikFaturaApp"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " [1/4] Next.js Frontend Statik Export Derlemesi Başlatılıyor...  " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

Push-Location $FrontendDir
try {
    Write-Host "Dizin: $FrontendDir" -ForegroundColor Gray
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "Frontend derlemesi başarısız oldu (Hata kodu: $LASTEXITCODE)."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $FrontendOutDir)) {
    throw "Frontend 'out' klasörü bulunamadı: $FrontendOutDir"
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [2/4] API Projesinde 'wwwroot' Klasörü Hazırlanıyor...           " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

if (Test-Path $WwwrootDir) {
    Write-Host "Mevcut wwwroot klasörü temizleniyor: $WwwrootDir" -ForegroundColor Yellow
    Remove-Item -Path $WwwrootDir -Recurse -Force
}
New-Item -ItemType Directory -Path $WwwrootDir -Force | Out-Null
Write-Host "wwwroot klasörü oluşturuldu." -ForegroundColor Green

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [3/4] Frontend Çıktıları wwwroot Klasörüne Kopyalanıyor...      " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

Copy-Item -Path "$FrontendOutDir\*" -Destination $WwwrootDir -Recurse -Force
Write-Host "Statik dosyalar wwwroot içerisine başarıyla kopyalandı." -ForegroundColor Green

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [4/4] Hedef Temizliği ve .NET Tek Dosya EXE Yayınlanıyor...     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Hedef klasör oluştur veya mevcutsa (wwwroot hariç) eski build artıklarını temizle
if (Test-Path $PublishOutputDir) {
    Write-Host "Hedef klasördeki eski derleme artıkları temizleniyor (wwwroot hariç): $PublishOutputDir" -ForegroundColor Yellow
    Get-ChildItem -Path $PublishOutputDir -Exclude "wwwroot" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $PublishOutputDir -Force | Out-Null
}

# Windows Server SCM için tam bağımsız (self-contained), tek parça .exe üretimi
dotnet publish $ApiProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $PublishOutputDir

if ($LASTEXITCODE -ne 0) {
    throw ".NET publish işlemi başarısız oldu (Hata kodu: $LASTEXITCODE)."
}

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host " TEBRİKLER! Yayınlama başarıyla tamamlandı.                      " -ForegroundColor Green
Write-Host " Çıktı Dizini: $PublishOutputDir                                 " -ForegroundColor Green
Write-Host " Çalıştırılabilir Dosya: $PublishOutputDir\InvoiceComparison.Api.exe" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green

