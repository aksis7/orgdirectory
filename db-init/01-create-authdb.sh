#!/usr/bin/env bash
set -euo pipefail

AUTH_DB="${AUTH_DB:-authdb}"

# создать БД authdb, если нет
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
DO \$\$ BEGIN
   IF NOT EXISTS (SELECT FROM pg_database WHERE datname = '${AUTH_DB}') THEN
      EXECUTE format('CREATE DATABASE %I OWNER %I', '${AUTH_DB}', '${POSTGRES_USER}');
   END IF;
END \$\$;
EOSQL

# накатить auth-схему в authdb
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$AUTH_DB" -f /docker-entrypoint-initdb.d/10-init-authdb.sql
