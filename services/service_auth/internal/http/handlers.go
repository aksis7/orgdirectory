package http

import (
	"context"
	"net/http"
	"strings"

	"github.com/gin-gonic/gin"

	"auth_project/internal/auth"
)

// RegisterRoutes configures authentication routes on the provided gin router.
// It expects an instance of auth.Service to execute business logic.
func RegisterRoutes(ctx context.Context, router *gin.Engine, svc *auth.Service) {
	// registration
	router.POST("/auth/register", func(c *gin.Context) {
		var req struct {
			Login    string `json:"login"  binding:"omitempty,alphanum,min=3,max=30"`
			Email    string `json:"email" binding:"required,email"`
			Password string `json:"password" binding:"required,min=6"`
		}
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}
		login := strings.ToLower(req.Login) // import "strings"
		if err := svc.Register(ctx, login, req.Email, req.Password); err != nil {
			c.JSON(http.StatusConflict, gin.H{"error": err.Error()})
			return
		}
		c.Status(http.StatusCreated)
	})
	// login
	router.POST("/auth/login", func(c *gin.Context) {
		var req struct {
			Login    string `json:"login" binding:"omitempty,alphanum,required_without=Email"`
			Email    string `json:"email" binding:"omitempty,email,required_without=Login"`
			Password string `json:"password" binding:"required"`
		}
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}
		//login := strings.ToLower(req.Login) // import "strings"
		//email := strings.ToLower(strings.TrimSpace(req.Email))
		ident := strings.ToLower(strings.TrimSpace(req.Email))
		if ident == "" {
			ident = strings.ToLower(strings.TrimSpace(req.Login))
		}

		tokens, err := svc.Login(c.Request.Context(), ident, req.Password)

		if err != nil {
			c.JSON(http.StatusUnauthorized, gin.H{"error": err.Error()})
			return
		}
		c.JSON(http.StatusOK, gin.H{"access_token": tokens.AccessToken, "refresh_token": tokens.RefreshToken})
	})
	// refresh token
	router.POST("/auth/refresh", func(c *gin.Context) {
		var req struct {
			RefreshToken string `json:"refresh_token" binding:"required"`
		}
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}
		tokens, err := svc.Refresh(ctx, req.RefreshToken)
		if err != nil {
			c.JSON(http.StatusUnauthorized, gin.H{"error": err.Error()})
			return
		}
		c.JSON(http.StatusOK, gin.H{"access_token": tokens.AccessToken, "refresh_token": tokens.RefreshToken})
	})
	// validate token
	router.POST("/auth/validate", func(c *gin.Context) {
		var req struct {
			AccessToken string `json:"access_token" binding:"required"`
		}
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}
		userID, err := svc.Validate(req.AccessToken)
		if err != nil {
			c.JSON(http.StatusUnauthorized, gin.H{"error": err.Error()})
			return
		}
		c.JSON(http.StatusOK, gin.H{"user_id": userID})
	})
}
