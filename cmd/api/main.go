package main

import (
	"context"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/go-chi/chi/v5"
	chimiddleware "github.com/go-chi/chi/v5/middleware"
	"github.com/joho/godotenv"

	"github.com/rafmachine/cpf-api/internal/handler"
	"github.com/rafmachine/cpf-api/internal/middleware"
	"github.com/rafmachine/cpf-api/internal/service"
)

func main() {
	// Load .env file if present (ignore error in production)
	_ = godotenv.Load()

	// Configure structured JSON logger
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	}))
	slog.SetDefault(logger)

	// Validate required environment variables
	apiToken := os.Getenv("API_TOKEN")
	if apiToken == "" {
		slog.Error("API_TOKEN environment variable is required")
		os.Exit(1)
	}

	receitaWSURL := os.Getenv("RECEITA_WS_URL")
	if receitaWSURL == "" {
		receitaWSURL = "https://www.receitaws.com.br/v1/cpf"
	}

	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	// Build dependencies
	cpfSvc := service.NewCPFService(receitaWSURL)
	cpfHandler := handler.NewCPFHandler(cpfSvc)

	// Set up router
	r := chi.NewRouter()

	// Global middlewares
	r.Use(chimiddleware.RequestID)
	r.Use(chimiddleware.RealIP)
	r.Use(structuredLogger())
	r.Use(chimiddleware.Recoverer)

	// Health check (no auth required)
	r.Get("/health", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte(`{"status":"ok"}`))
	})

	// Protected routes
	r.Group(func(r chi.Router) {
		r.Use(middleware.BearerAuth)
		r.Get("/consulta/cpf/{cpf}", cpfHandler.Consultar)
	})

	srv := &http.Server{
		Addr:         ":" + port,
		Handler:      r,
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 30 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	// Graceful shutdown
	done := make(chan os.Signal, 1)
	signal.Notify(done, os.Interrupt, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		slog.Info("server starting", "port", port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			slog.Error("server error", "error", err)
			os.Exit(1)
		}
	}()

	<-done
	slog.Info("server shutting down")

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		slog.Error("shutdown error", "error", err)
	}

	slog.Info("server stopped")
}

// structuredLogger returns a chi middleware that emits structured log entries.
func structuredLogger() func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			ww := chimiddleware.NewWrapResponseWriter(w, r.ProtoMajor)
			start := time.Now()

			defer func() {
				slog.Info("request",
					"method", r.Method,
					"path", r.URL.Path,
					"status", ww.Status(),
					"bytes", ww.BytesWritten(),
					"duration_ms", time.Since(start).Milliseconds(),
					"request_id", chimiddleware.GetReqID(r.Context()),
					"remote", r.RemoteAddr,
				)
			}()

			next.ServeHTTP(ww, r)
		})
	}
}
