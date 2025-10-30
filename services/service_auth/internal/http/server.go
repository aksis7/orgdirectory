package http

import (
	"context"
	"log"
	"os"

	"github.com/gin-gonic/gin"

	"auth_project/internal/auth"
)

// Start runs the HTTP server on the configured port. It creates a gin
// engine, registers routes and listens until stopped. Environment
// variables AUTH_SECRET and AUTH_ISSUER can override defaults.
func Start(ctx context.Context, svc *auth.Service) error {
	router := gin.Default()
	RegisterRoutes(ctx, router, svc)
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}
	log.Printf("HTTP server listening on :%s", port)
	return router.Run(":" + port)
}
