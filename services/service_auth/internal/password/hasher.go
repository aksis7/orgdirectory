package password

import "golang.org/x/crypto/bcrypt"

// Hasher defines an abstraction for hashing and verifying passwords.
// In the PDF it is recommended to use bcrypt or Argon2【471101221547741†screenshot】.
type Hasher interface {
    HashPassword(password string) (string, error)
    CompareHashAndPassword(hashedPassword, password string) error
}

// BcryptHasher implements Hasher using bcrypt.
type BcryptHasher struct{}

// HashPassword hashes the plaintext password with bcrypt.DefaultCost.
func (BcryptHasher) HashPassword(password string) (string, error) {
    b, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
    if err != nil {
        return "", err
    }
    return string(b), nil
}

// CompareHashAndPassword compares the stored hash with the provided password.
func (BcryptHasher) CompareHashAndPassword(hashedPassword, password string) error {
    return bcrypt.CompareHashAndPassword([]byte(hashedPassword), []byte(password))
}