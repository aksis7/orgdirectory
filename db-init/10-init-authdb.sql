-- users + refresh_tokens для auth_service
CREATE TABLE IF NOT EXISTS users (
  id            TEXT PRIMARY KEY,
  login         TEXT UNIQUE NOT NULL,
  email         TEXT UNIQUE NOT NULL,
  password_hash TEXT NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uniq_users_email_lower ON users (LOWER(email));
CREATE INDEX IF NOT EXISTS idx_users_email_lower ON users ((LOWER(email)));

CREATE TABLE IF NOT EXISTS refresh_tokens (
  token      TEXT PRIMARY KEY,
  user_id    TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  expires_at TIMESTAMPTZ NOT NULL,
  revoked    BOOLEAN NOT NULL DEFAULT FALSE
);
