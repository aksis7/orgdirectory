package jwt

import (
	"context"
	"errors"
	"fmt"
	"time"

	"github.com/golang-jwt/jwt/v5"
)

// Service issues and verifies JWTs. For brevity, a single HMAC secret is
// used. Production deployments should prefer asymmetric algorithms
// (RS256/ES256/EdDSA) and secure key storage【401677182695602†screenshot】.
type Service struct {
	secret     []byte
	issuer     string
	accessTTL  time.Duration
	refreshTTL time.Duration
}

// New constructs a new JWT service.
func New(secret, issuer string, accessTTL, refreshTTL time.Duration) *Service {
	return &Service{
		secret:     []byte(secret),
		issuer:     issuer,
		accessTTL:  accessTTL,
		refreshTTL: refreshTTL,
	}
}

// Tokens holds the issued access and refresh tokens and the expiry of
// refresh token. Access tokens expire sooner than refresh tokens.
type Tokens struct {
	AccessToken   string
	RefreshToken  string
	RefreshExpiry time.Time
}

// Issue generates a new pair of access and refresh tokens for the given
// user ID【471101221547741†screenshot】.
func (s *Service) Issue(ctx context.Context, userID string) (*Tokens, error) {
	now := time.Now()
	// build access token
	accessClaims := jwt.MapClaims{
		"iss": s.issuer,
		"sub": userID,
		"aud": "auth_service",
		"iat": now.Unix(),
		"exp": now.Add(s.accessTTL).Unix(),
		// additional claims (roles/scopes) could go here【809718908566546†screenshot】
	}
	access := jwt.NewWithClaims(jwt.SigningMethodHS256, accessClaims)
	accessStr, err := access.SignedString(s.secret)
	if err != nil {
		return nil, err
	}
	// build refresh token (also JWT for simplicity)
	refreshClaims := jwt.MapClaims{
		"sub": userID,
		"iat": now.Unix(),
		"exp": now.Add(s.refreshTTL).Unix(),
	}
	refresh := jwt.NewWithClaims(jwt.SigningMethodHS256, refreshClaims)
	refreshStr, err := refresh.SignedString(s.secret)
	if err != nil {
		return nil, err
	}
	return &Tokens{AccessToken: accessStr, RefreshToken: refreshStr, RefreshExpiry: now.Add(s.refreshTTL)}, nil
}

// ValidateAccess verifies the access token and returns the subject (user ID).
func (s *Service) ValidateAccess(tokenStr string) (string, error) {
	token, err := jwt.Parse(tokenStr, func(t *jwt.Token) (any, error) {
		// ensure HMAC method
		if _, ok := t.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("unexpected signing method: %v", t.Header["alg"])
		}
		return s.secret, nil
	})
	if err != nil {
		return "", err
	}
	if !token.Valid {
		return "", errors.New("invalid token")
	}
	claims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		return "", errors.New("invalid claims")
	}
	sub, ok := claims["sub"].(string)
	if !ok {
		return "", errors.New("missing sub")
	}
	return sub, nil
}
