package store

import (
	"context"
	"errors"
	"fmt"
	"time"

	"auth_project/internal/domain"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

// PgStore implements UserStore using PostgreSQL via pgx. It requires
// tables `users` and `refresh_tokens` with appropriate columns. A sample
// schema:
//
// CREATE TABLE users (
//
//	id TEXT PRIMARY KEY,
//	email TEXT UNIQUE NOT NULL,
//	password TEXT NOT NULL
//
// );
//
// CREATE TABLE refresh_tokens (
//
//	token TEXT PRIMARY KEY,
//	user_id TEXT NOT NULL REFERENCES users(id),
//	expires_at TIMESTAMP NOT NULL,
//	revoked BOOLEAN NOT NULL DEFAULT FALSE
//
// );
//
// This implementation opens a connection pool on creation and uses
// context.Background for queries; in production you would pass a
// context from request handlers.
type PgStore struct {
	pool *pgxpool.Pool
}

// NewPgStore connects to the Postgres database using the provided DSN.
func NewPgStore(dsn string) (*PgStore, error) {
	pool, err := pgxpool.New(context.Background(), dsn)
	if err != nil {
		return nil, err
	}
	return &PgStore{pool: pool}, nil
}

// Close releases the connection pool.
func (p *PgStore) Close() {
	p.pool.Close()
}

func (p *PgStore) CreateUser(ctx context.Context, u *domain.User) error {
	ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
	defer cancel()
	_, err := p.pool.Exec(
		ctx,
		`INSERT INTO users (id, login, email, password_hash, created_at)
         VALUES ($1, $2, $3, $4, $5)`,
		u.ID,
		u.Login,
		u.Email,
		u.PasswordHash, //  hash
		time.Now(),
	)
	if err != nil {
		return fmt.Errorf("create user: %w", err)
	}
	return nil
}

func (p *PgStore) FindByEmail(ctx context.Context, email string) (*domain.User, error) {
	ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
	defer cancel()
	row := p.pool.QueryRow(ctx, `SELECT id, login, email, password_hash, created_at FROM users WHERE LOWER(email) = LOWER($1)`, email)
	var u domain.User
	if err := row.Scan(&u.ID, &u.Login, &u.Email, &u.PasswordHash, &u.CreatedAt); err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, nil
		}
		return nil, err
	}
	return &u, nil
}

func (p *PgStore) SaveRefreshToken(ctx context.Context, token string, rec RefreshRecord) error {
	ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
	defer cancel()
	_, err := p.pool.Exec(ctx, `INSERT INTO refresh_tokens (token, user_id, expires_at, revoked) VALUES ($1, $2, $3, $4)`, token, rec.UserID, rec.ExpiresAt, rec.Revoked)
	return err
}

func (p *PgStore) GetRefreshToken(ctx context.Context, token string) (*RefreshRecord, error) {
	ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
	defer cancel()
	row := p.pool.QueryRow(ctx, `SELECT user_id, expires_at, revoked FROM refresh_tokens WHERE token = $1`, token)
	var rec RefreshRecord
	if err := row.Scan(&rec.UserID, &rec.ExpiresAt, &rec.Revoked); err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, nil
		}
		return nil, err
	}
	return &rec, nil
}
func (p *PgStore) FindByLogin(ctx context.Context, login string) (domain.User, error) {
	row := p.pool.QueryRow(ctx,
		`SELECT id, login, email, password_hash, created_at
		   FROM users
		  WHERE login = $1`, login)

	var u domain.User
	if err := row.Scan(&u.ID, &u.Login, &u.Email, &u.PasswordHash, &u.CreatedAt); err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return domain.User{}, nil // не найдено
		}
		return domain.User{}, fmt.Errorf("find by login: %w", err)
	}
	return u, nil
}
func (p *PgStore) RevokeRefreshToken(ctx context.Context, token string) error {
	ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
	defer cancel()
	_, err := p.pool.Exec(ctx, `UPDATE refresh_tokens SET revoked = TRUE WHERE token = $1`, token)
	return err
}
