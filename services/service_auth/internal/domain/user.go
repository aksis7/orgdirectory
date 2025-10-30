package domain

import "time"

// User represents an account in the authentication domain. In a more
// complete implementation this model would include additional fields such
// as roles, metadata, and timestamps. Claims for RBAC and scopes are
// usually stored either here or encoded in JWTs【809718908566546†screenshot】.
//
// ID should be unique across users; in production a UUID/ULID would be
// appropriate.
type User struct {
	ID           string
	Login        string
	Email        string
	PasswordHash string    // hashed password
	CreatedAt    time.Time `json:"created_at"`
	// Roles or scopes could be added here
}
