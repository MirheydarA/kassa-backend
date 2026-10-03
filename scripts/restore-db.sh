#!/usr/bin/env bash
# Kassa DB-ni OneDrive-dakı bir backup-dan bərpa edir.
#
# İstifadə:
#   ./scripts/restore-db.sh --list           # OneDrive-dakı mövcud backup-ları göstər
#   ./scripts/restore-db.sh <fayl_adı.bak>   # həmin backup-ı bərpa et
#
# DİQQƏT: Bərpa mövcud KassaDb bazasını TAM ƏVƏZ EDİR (WITH REPLACE).
# Əməliyyat təsdiq istəyir. Eyni serverə/container-ə bərpa üçün nəzərdə
# tutulub - fərqli bir serverə bərpa edəcəksənsə, fayl yollarının (MDF/LDF)
# uyğunluğunu yoxla (lazım gələrsə RESTORE sorğusuna MOVE əlavə et).

set -euo pipefail

CONTAINER_NAME="kassa-sqlserver"       # production-da fərqlidirsə dəyiş (docker ps)
DB_NAME="KassaDb"
REMOTE="onedrive:KassaBackups"
LOCAL_TMP_DIR="/tmp/kassa-restore"

if [ "${1:-}" = "--list" ] || [ "${1:-}" = "" ]; then
  echo "OneDrive-dakı backup-lar:"
  rclone lsf "$REMOTE" --format "tsp" | sort
  echo ""
  echo "İstifadə: $0 <fayl_adı.bak>"
  exit 0
fi

FILE_NAME="$1"

SA_PASSWORD=$(docker exec "$CONTAINER_NAME" printenv MSSQL_SA_PASSWORD)
if [ -z "$SA_PASSWORD" ]; then
  echo "XƏTA: MSSQL_SA_PASSWORD container-dən oxunmadı (container: $CONTAINER_NAME)"
  exit 1
fi

echo "DİQQƏT: Bu əməliyyat mövcud '$DB_NAME' bazasını '$FILE_NAME' backup-ı ilə TAM ƏVƏZ EDƏCƏK."
read -r -p "Davam etmək istəyirsən? (bəli/xeyr): " confirm
if [ "$confirm" != "bəli" ]; then
  echo "Ləğv edildi."
  exit 1
fi

mkdir -p "$LOCAL_TMP_DIR"
echo "OneDrive-dan endirilir: $FILE_NAME"
rclone copy "$REMOTE/$FILE_NAME" "$LOCAL_TMP_DIR/"

echo "Container-ə köçürülür..."
docker exec "$CONTAINER_NAME" mkdir -p /var/opt/mssql/restore
docker cp "$LOCAL_TMP_DIR/$FILE_NAME" "$CONTAINER_NAME:/var/opt/mssql/restore/$FILE_NAME"
rm -f "$LOCAL_TMP_DIR/$FILE_NAME"

echo "Baza bərpa olunur (WITH REPLACE)..."
docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DB_NAME] FROM DISK = N'/var/opt/mssql/restore/$FILE_NAME' WITH REPLACE; ALTER DATABASE [$DB_NAME] SET MULTI_USER;"

docker exec "$CONTAINER_NAME" rm -f "/var/opt/mssql/restore/$FILE_NAME"

echo "Bərpa tamamlandı: $FILE_NAME"
echo "Backend-i yenidən başlatmaq tövsiyə olunur: docker compose restart backend"
