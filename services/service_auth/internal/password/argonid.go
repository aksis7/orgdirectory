package password

import (
	"crypto/rand"
	"crypto/subtle"
	"encoding/base64"
	"errors"
	"fmt"
	"strconv"
	"strings"

	"golang.org/x/crypto/argon2"
)

type Argon2idHasher struct{}

const (
	defTime     = uint32(3)
	defMemory   = uint32(64 * 1024) // 64 MiB
	defParallel = uint8(1)
	defSaltLen  = 16
	defKeyLen   = 32
)

// $argon2id$v=19$m=65536,t=3,p=1$<b64(salt)>$<b64(hash)>
func (Argon2idHasher) HashPassword(pw string) (string, error) {
	salt := make([]byte, defSaltLen)
	if _, err := rand.Read(salt); err != nil {
		return "", fmt.Errorf("salt: %w", err)
	}
	hash := argon2.IDKey([]byte(pw), salt, defTime, defMemory, defParallel, defKeyLen)
	b64 := base64.RawStdEncoding
	return fmt.Sprintf("$argon2id$v=19$m=%d,t=%d,p=%d$%s$%s",
		defMemory, defTime, defParallel,
		b64.EncodeToString(salt),
		b64.EncodeToString(hash),
	), nil
}

func (Argon2idHasher) CompareHashAndPassword(encodedHash, pw string) error {
	parts := strings.Split(encodedHash, "$")
	// ["", "argon2id", "v=19", "m=..,t=..,p=..", "<salt>", "<hash>"]
	if len(parts) != 6 || parts[1] != "argon2id" {
		return errors.New("invalid argon2id hash")
	}
	// параметры
	var mem uint32
	var tc uint32
	var par uint8
	for _, kv := range strings.Split(parts[3], ",") {
		switch {
		case strings.HasPrefix(kv, "m="):
			v, err := strconv.ParseUint(strings.TrimPrefix(kv, "m="), 10, 32)
			if err != nil {
				return errors.New("bad memory param")
			}
			mem = uint32(v)
		case strings.HasPrefix(kv, "t="):
			v, err := strconv.ParseUint(strings.TrimPrefix(kv, "t="), 10, 32)
			if err != nil {
				return errors.New("bad time param")
			}
			tc = uint32(v)
		case strings.HasPrefix(kv, "p="):
			v, err := strconv.ParseUint(strings.TrimPrefix(kv, "p="), 10, 8)
			if err != nil {
				return errors.New("bad parallelism param")
			}
			par = uint8(v)
		}
	}
	if mem == 0 || tc == 0 || par == 0 {
		return errors.New("missing hash params")
	}

	b64 := base64.RawStdEncoding
	salt, err := b64.DecodeString(parts[4])
	if err != nil { // на всякий: попробуем с паддингом
		if salt, err = base64.StdEncoding.DecodeString(parts[4]); err != nil {
			return errors.New("bad salt encoding")
		}
	}
	exp, err := b64.DecodeString(parts[5])
	if err != nil {
		if exp, err = base64.StdEncoding.DecodeString(parts[5]); err != nil {
			return errors.New("bad hash encoding")
		}
	}

	key := argon2.IDKey([]byte(pw), salt, tc, mem, par, uint32(len(exp)))
	if subtle.ConstantTimeCompare(key, exp) == 1 {
		return nil
	}
	return errors.New("password mismatch")
}
