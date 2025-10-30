package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"auth_project/internal/auth"
	event "auth_project/internal/event"
	httptransport "auth_project/internal/http" // и этот, чтобы не путать со std net/http
	"auth_project/internal/jwt"
	"auth_project/internal/password"
	"auth_project/internal/store"
)

func main() {
	// Контекст завершения от SIGINT/SIGTERM
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	// Конфигурация
	secret := os.Getenv("AUTH_SECRET")
	if secret == "" {
		secret = "insecure-default-secret"
	}
	issuer := os.Getenv("AUTH_ISSUER")
	if issuer == "" {
		issuer = "auth_service"
	}
	accessTTL := 15 * time.Minute
	refreshTTL := 24 * time.Hour

	// Выбор UserStore
	var userStore store.UserStore
	dsn := os.Getenv("DB_DSN")
	if dsn != "" {
		pgStore, err := store.NewPgStore(dsn)
		if err != nil {
			log.Fatalf("failed to connect to postgres: %v", err)
		}
		userStore = pgStore
		defer pgStore.Close()
		log.Printf("using Postgres user store")
	} else {
		userStore = store.NewMemStore()
		log.Printf("using in-memory user store")
	}

	// Hasher + JWT
	hasher := password.Argon2idHasher{}
	jwtSvc := jwt.New(secret, issuer, accessTTL, refreshTTL)

	// Паблишер событий
	var publisher event.Publisher
	if brokersEnv := os.Getenv("KAFKA_BROKERS"); brokersEnv != "" {
		var brokers []string
		for _, part := range strings.Split(brokersEnv, ",") {
			if b := strings.TrimSpace(part); b != "" {
				brokers = append(brokers, b)
			}
		}
		topic := os.Getenv("KAFKA_TOPIC")
		if topic == "" {
			topic = "auth-events"
		}
		publisher = event.NewKafkaPublisher(brokers, topic)
		log.Printf("using Kafka publisher on topic %s", topic)
		defer func() {
			if kp, ok := publisher.(*event.KafkaPublisher); ok {
				_ = kp.Close()
			}
		}()
	} else {
		publisher = event.StdoutPublisher{}
		log.Printf("using stdout publisher")
	}

	// Собираем сервис
	svc := auth.New(userStore, hasher, jwtSvc, publisher)

	// Запуск HTTP
	if err := httptransport.Start(ctx, svc); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
