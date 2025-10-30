package store

import (
	"auth_project/internal/domain"
	"context"
	"errors"
	"fmt"
	"sync"
	"time"
)

var ErrNotFound = errors.New("not found")

// RefreshRecord stores information about a refresh token's validity and
// owner. This facilitates token rotation and revocation【401677182695602†screenshot】.
type RefreshRecord struct {
	UserID    string
	ExpiresAt time.Time
	Revoked   bool
}

// UserStore defines an abstraction over persistent storage for users and
// refresh tokens【319062519378981†screenshot】. Implementations could use
// SQL (PostgreSQL with pgx or GORM), NoSQL (MongoDB) or an in-memory
// cache (Redis). For demonstration, an in-memory implementation is
// provided.
type UserStore interface {
	// CreateUser stores a new user; returns an error if email already exists.
	CreateUser(ctx context.Context, u *domain.User) error
	// FindByEmail fetches a user by email; returns nil if not found.
	FindByLogin(ctx context.Context, login string) (domain.User, error) // <-- добавить
	FindByEmail(ctx context.Context, email string) (*domain.User, error)
	// SaveRefreshToken persists a refresh token record.
	SaveRefreshToken(ctx context.Context, token string, rec RefreshRecord) error
	// GetRefreshToken returns the refresh token record if present.
	GetRefreshToken(ctx context.Context, token string) (*RefreshRecord, error)
	// RevokeRefreshToken marks a refresh token as revoked.
	RevokeRefreshToken(ctx context.Context, token string) error
}

// =====================
// In-memory implementation
// =====================

// MemStore is a thread-safe in-memory implementation of UserStore. It is
// suitable for testing and demo purposes but not for production.
type MemStore struct {
	mu      sync.RWMutex
	byLogin map[string]domain.User    // login -> user (значение)
	byEmail map[string]string         // email -> login
	refresh map[string]*RefreshRecord // refreshID -> запись
}

func NewMemStore() *MemStore {
	return &MemStore{
		byLogin: make(map[string]domain.User),
		byEmail: make(map[string]string),
		refresh: make(map[string]*RefreshRecord),
	}
}

func (s *MemStore) CreateUser(ctx context.Context, u *domain.User) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if _, ok := s.byLogin[u.Login]; ok {
		return fmt.Errorf("user with login %s exists", u.Login)
	}
	if _, ok := s.byEmail[u.Email]; ok {
		return fmt.Errorf("user with email %s exists", u.Email)
	}
	if u.CreatedAt.IsZero() {
		u.CreatedAt = time.Now()
	}
	// сохраняем копию
	s.byLogin[u.Login] = *u
	s.byEmail[u.Email] = u.Login
	return nil
}

func (s *MemStore) FindByLogin(ctx context.Context, login string) (domain.User, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	if u, ok := s.byLogin[login]; ok {
		return u, nil
	}
	return domain.User{}, nil // не найдено
}

func (s *MemStore) FindByEmail(ctx context.Context, email string) (*domain.User, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	login, ok := s.byEmail[email] // <-- строка
	if !ok {
		return nil, nil
	}
	u := s.byLogin[login] // значение
	uu := u               // делаем адресуемую копию
	return &uu, nil
}

func (s *MemStore) SaveRefreshToken(ctx context.Context, token string, rec RefreshRecord) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	r := rec
	s.refresh[token] = &r
	return nil
}

func (s *MemStore) GetRefreshToken(ctx context.Context, token string) (*RefreshRecord, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	rec, ok := s.refresh[token]
	if !ok {
		return nil, ErrNotFound
	}
	r := *rec
	return &r, nil
}

func (s *MemStore) RevokeRefreshToken(ctx context.Context, token string) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	rec, ok := s.refresh[token]
	if !ok {
		return ErrNotFound
	}
	rec.Revoked = true
	return nil
}
