#!/bin/bash
set -e

APP_NAME="tronpanel"
APP_DIR="/opt/$APP_NAME"
LOG_DIR="/var/log/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"

echo "--- VM Hazırlanıyor: $APP_NAME ---"

# 1. .NET 10 Runtime Kurulumu
echo "1. .NET 10 Runtime kontrol ediliyor..."
if ! command -v dotnet &> /dev/null || [[ $(dotnet --version) != 10.* ]]; then
    echo ".NET 10 Runtime kuruluyor..."
    # Debian 12 için Microsoft repository ekleme
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt update
    sudo apt install -y dotnet-runtime-10.0
else
    echo ".NET 10 Runtime zaten kurulu."
fi

# 2. Dizinleri oluştur
echo "2. Dizinler oluşturuluyor..."
sudo mkdir -p $APP_DIR
sudo mkdir -p $LOG_DIR
sudo chown -R $USER:$USER $APP_DIR
sudo chown -R $USER:$USER $LOG_DIR

# 3. Systemd Servis Dosyası Oluştur
echo "3. Systemd servis dosyası yapılandırılıyor..."
sudo bash -c "cat > $SERVICE_FILE" <<EOF
[Unit]
Description=Tron Panel Cekme Application
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/TRONPANELE_CEKME.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME
User=$USER
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
EOF

# 4. Servisi Yenile
echo "4. Servisler yenileniyor..."
sudo systemctl daemon-reload
sudo systemctl enable $APP_NAME.service

echo "--- VM Hazırlığı Tamamlandı! ---"
