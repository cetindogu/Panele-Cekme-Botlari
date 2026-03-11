#!/bin/bash
set -e

APP_NAME="tronpanel"
APP_DIR="/opt/$APP_NAME"
LOG_DIR="/var/log/$APP_NAME"

echo "--- VM Hazırlanıyor: $APP_NAME ---"

# 1. Dizinleri oluştur
echo "1. Dizinler oluşturuluyor..."
sudo mkdir -p $APP_DIR
sudo mkdir -p $LOG_DIR
sudo chown -R $USER:$USER $APP_DIR
sudo chown -R $USER:$USER $LOG_DIR

echo "--- VM Hazırlığı Tamamlandı! ---"
