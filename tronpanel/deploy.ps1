# Tron Panel Cekme Deployment Script (Windows 11)

$PROJECT_DIR = "TRONPANELE_CEKME"
$PUBLISH_DIR = "$PROJECT_DIR\bin\Release\net10.0\publish"
$GCLOUD_PATH = "$env:LOCALAPPDATA\Google\Cloud SDK\google-cloud-sdk\bin\gcloud.cmd"
$VM_NAME = "instance-20260308-013736"
$VM_USER = "cetindogu"
$REMOTE_APP_DIR = "/opt/tronpanel"
$REMOTE_SETUP_SCRIPT = "setup-vm.sh"

Write-Host "--- Deployment Başlatılıyor ---" -ForegroundColor Cyan

# Check for authentication
& $GCLOUD_PATH config list account --format="value(core.account)" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "GCloud account not found! Running 'gcloud auth login'..." -ForegroundColor Yellow
    & $GCLOUD_PATH auth login
}

# Check for project
$PROJECT_ID = & $GCLOUD_PATH config get-value project
if ([string]::IsNullOrEmpty($PROJECT_ID)) {
    Write-Host "GCloud project not set! Running 'gcloud projects list'..." -ForegroundColor Yellow
    & $GCLOUD_PATH projects list
    $PROJECT_ID = Read-Host "Lütfen GCP Project ID girin"
    & $GCLOUD_PATH config set project $PROJECT_ID
}

# 1. Kodu Build Et
Write-Host "1. Proje build ediliyor (net10.0)..."
dotnet publish $PROJECT_DIR -c Release -r linux-x64 --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build hatası oluştu!"
    exit $LASTEXITCODE
}

# 2. VM'i Hazırla (İlk seferde veya güncelleme gerektiğinde)
Write-Host "2. VM hazırlık scripti gönderiliyor ve çalıştırılıyor..."
& $GCLOUD_PATH compute scp $REMOTE_SETUP_SCRIPT "${VM_USER}@${VM_NAME}:." --zone=us-west1-b
if ($LASTEXITCODE -ne 0) {
    Write-Warning "SCP hatası! Lütfen gcloud auth login ve gcloud config set project komutlarını çalıştırdığınızdan emin olun."
    exit $LASTEXITCODE
}

& $GCLOUD_PATH compute ssh "${VM_USER}@${VM_NAME}" --zone=us-west1-b --command="chmod +x ./$REMOTE_SETUP_SCRIPT && ./$REMOTE_SETUP_SCRIPT"

# 3. Kodu VM'e Kopyala
Write-Host "3. Yeni sürüm VM'e kopyalanıyor..."
# Önce eski dosyaları temizle (isteğe bağlı, ama temiz kurulum iyidir)
& $GCLOUD_PATH compute ssh "${VM_USER}@${VM_NAME}" --zone=us-west1-b --command="sudo systemctl stop tronpanel.service && sudo rm -rf $REMOTE_APP_DIR/*"

# Dosyaları gönder
& $GCLOUD_PATH compute scp --recurse "$PUBLISH_DIR/*" "${VM_USER}@${VM_NAME}:${REMOTE_APP_DIR}/" --zone=us-west1-b

# 4. Servisi Başlat
Write-Host "4. Servis başlatılıyor..."
& $GCLOUD_PATH compute ssh "${VM_USER}@${VM_NAME}" --zone=us-west1-b --command="sudo systemctl start tronpanel.service && sudo systemctl status tronpanel.service"

Write-Host "--- Deployment Tamamlandı! ---" -ForegroundColor Green
