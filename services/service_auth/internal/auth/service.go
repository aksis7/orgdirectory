package auth

import (
	"auth_project/internal/domain"
	"auth_project/internal/event"
	"auth_project/internal/jwt"
	"auth_project/internal/password"
	"auth_project/internal/store"
	"context"
	"errors"
	"fmt"
	"strings"
	"time"
)

// Service implements the core authentication logic: registration, login,
// token refresh and validation. It coordinates user storage, password
// hashing, JWT issuance and event publishing【351230703904074†screenshot】.
type Service struct {
	users  store.UserStore
	hasher password.Hasher
	tokens *jwt.Service
	events event.Publisher
}

// New constructs a new Service.
func New(users store.UserStore, hasher password.Hasher, tokens *jwt.Service, events event.Publisher) *Service {
	return &Service{users: users, hasher: hasher, tokens: tokens, events: events}
}

// Register creates a new user with a hashed password and publishes an event.
func (s *Service) Register(ctx context.Context, login, email, plaintext string) error {
	hashed, err := s.hasher.HashPassword(plaintext)
	if err != nil {
		return err
	}
	u := &domain.User{ID: generateID(), Login: login, Email: email, PasswordHash: hashed}
	if err := s.users.CreateUser(ctx, u); err != nil {
		return err
	}
	s.events.Publish("USER_REGISTERED", map[string]any{"userID": u.ID, "email": u.Email})
	return nil
}

// Login authenticates the user and issues tokens. It publishes
// LOGIN_SUCCESS or LOGIN_FAILED events depending on outcome【471101221547741†screenshot】.
func (s *Service) Login(ctx context.Context, ident, plaintext string) (*jwt.Tokens, error) {
	var u *domain.User
	if strings.Contains(ident, "@") {
		// по email
		user, err := s.users.FindByEmail(ctx, ident)
		if err != nil {
			s.events.Publish("LOGIN_FAILED", map[string]any{"email": ident, "error": err.Error()})
			return nil, errors.New("authentication failed")
		}
		if user == nil {
			s.events.Publish("LOGIN_FAILED", map[string]any{"email": ident, "error": "user not found"})
			return nil, errors.New("authentication failed")
		}
		u = user
	} else {
		// по login
		user, err := s.users.FindByLogin(ctx, ident)
		if err != nil {
			s.events.Publish("LOGIN_FAILED", map[string]any{"login": ident, "error": err.Error()})
			return nil, errors.New("authentication failed")
		}
		if user.ID == "" {
			s.events.Publish("LOGIN_FAILED", map[string]any{"login": ident, "error": "user not found"})
			return nil, errors.New("authentication failed")
		}
		u = &user
	}

	if u == nil {
		return nil, errors.New("authentication failed")
	}
	if err := s.hasher.CompareHashAndPassword(u.PasswordHash, plaintext); err != nil {
		s.events.Publish("LOGIN_FAILED", map[string]any{"userID": u.ID, "error": "incorrect password"})
		return nil, errors.New("authentication failed")
	}
	tokens, err := s.tokens.Issue(ctx, u.ID)
	if err != nil {
		return nil, err
	}
	rec := store.RefreshRecord{UserID: u.ID, ExpiresAt: tokens.RefreshExpiry, Revoked: false}
	if err := s.users.SaveRefreshToken(ctx, tokens.RefreshToken, rec); err != nil {
		return nil, err
	}
	s.events.Publish("LOGIN_SUCCESS", map[string]any{"userID": u.ID})
	return tokens, nil
}

// Refresh validates the old refresh token, revokes it and issues new tokens.
func (s *Service) Refresh(ctx context.Context, refreshToken string) (*jwt.Tokens, error) {
	rec, err := s.users.GetRefreshToken(ctx, refreshToken)
	if err != nil || rec == nil {
		return nil, errors.New("invalid refresh token")
	}
	if rec.Revoked || time.Now().After(rec.ExpiresAt) {
		return nil, errors.New("invalid refresh token")
	}
	// revoke the old token
	_ = s.users.RevokeRefreshToken(ctx, refreshToken)
	// issue new tokens
	tokens, err := s.tokens.Issue(ctx, rec.UserID)
	if err != nil {
		return nil, err
	}
	newRec := store.RefreshRecord{UserID: rec.UserID, ExpiresAt: tokens.RefreshExpiry, Revoked: false}
	if err := s.users.SaveRefreshToken(ctx, tokens.RefreshToken, newRec); err != nil {
		return nil, err
	}
	s.events.Publish("TOKEN_REFRESHED", map[string]any{"userID": rec.UserID})
	return tokens, nil
}

// Validate verifies the access token and returns the associated user ID.
func (s *Service) Validate(accessToken string) (string, error) {
	return s.tokens.ValidateAccess(accessToken)
}

// generateID creates a unique identifier using current time. Use
// UUID/ULID libraries for production.
func generateID() string {
	return fmt.Sprintf("u-%d", time.Now().UnixNano())
}
