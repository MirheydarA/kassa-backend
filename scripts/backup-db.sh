#!/usr/bin/env bash
# Kassa DB-nin avtomatik backup-ı: SQL Server container-dən backup alır,
# Google Drive-a göndərir, köhnə (RETENTION_DAYS-dən artıq) faylları silir.
#
# Bir dəfəlik qurulum (server üzərində):
#   1) rclone qur:        curl https://rclone.org/install.sh | sudo bash
#   2) Google Drive remote-u:  rclone config
#        - "n" (new remote), ad: gdrive, tip: Google Drive
#        - scope sualında "1" (full access) seç
#        - server-də brauzer olmadığı üçün "auto config" sualına "n" cavabı ver,
#          təlimat verəcək: öz noutbukunda "rclone authorize \"drive\"" işlət,
#          brauzerdə Google hesabınla daxil ol, çıxan token-i serverdəki
#          suala yapışdır (paste et)
#        - "Configure as a Shared Drive?" sualına "n" de (adi Drive kifayətdir)
#   3) Bu skripti serverə gətir (git pull artıq gətirəcək) və icazə ver:
#        chmod +x scripts/backup-db.sh
#   4) CONTAINER_NAME aşağıda production-dakı SQL Server container adı ilə
#      uyğun olduğunu yoxla (docker ps ilə bax).
#   5) Əllə bir dəfə test et: ./scripts/backup-db.sh && cat ~/kassa-backup.log
#   6) Cron-a əlavə et (günə 3 dəfə, məs. 02:00/10:00/18:00):
#        crontab -e
#        0 2,10,18 * * * /home/<user>/apps/kassa/backend/kassa-backend/scripts/backup-db.sh >> /home/<user>/kassa-backup-cron.log 2>&1

set -euo pipefail

# ---- Konfiqurasiya ----
CONTAINER_NAME="kassa-sqlserver"       # production-da fərqlidirsə dəyiş (docker ps)
DB_NAME="KassaDb"
REMOTE="gdrive:KassaBackups"           # rclone remote adı + Google Drive-dakı qovluq
RETENTION_DAYS=30
BACKUP_DIR_IN_CONTAINER="/var/opt/mssql/backup"
LOCAL_TMP_DIR="/tmp/kassa-backup"
LOG_FILE="$HOME/kassa-backup.log"

timestamp() { date "+%Y-%m-%d %H:%M:%S"; }
log() { echo "[$(timestamp)] $*" >> "$LOG_FILE"; }

trap 'log "XƏTA baş verdi (sətir $LINENO) - backup dayandırıldı"' ERR

log "Backup başladı"

SA_PASSWORD=$(docker exec "$CONTAINER_NAME" printenv MSSQL_SA_PASSWORD)
if [ -z "$SA_PASSWORD" ]; then
  log "XƏTA: MSSQL_SA_PASSWORD container-dən oxunmadı (container: $CONTAINER_NAME)"
  exit 1
fi

FILE_NAME="${DB_NAME}_$(date +%Y%m%d_%H%M%S).bak"

docker exec "$CONTAINER_NAME" mkdir -p "$BACKUP_DIR_IN_CONTAINER"

docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$BACKUP_DIR_IN_CONTAINER/$FILE_NAME' WITH FORMAT, INIT"

mkdir -p "$LOCAL_TMP_DIR"
docker cp "$CONTAINER_NAME:$BACKUP_DIR_IN_CONTAINER/$FILE_NAME" "$LOCAL_TMP_DIR/$FILE_NAME"
docker exec "$CONTAINER_NAME" rm -f "$BACKUP_DIR_IN_CONTAINER/$FILE_NAME"

rclone copy "$LOCAL_TMP_DIR/$FILE_NAME" "$REMOTE"
rm -f "$LOCAL_TMP_DIR/$FILE_NAME"

rclone delete --min-age "${RETENTION_DAYS}d" "$REMOTE"

log "Backup uğurla tamamlandı: $FILE_NAME"
