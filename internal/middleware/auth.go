package middleware

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"os"
	"strings"
)

type errorResponse struct {
	Error string `json:"error"`
}

// BearerAuth validates the Authorization header against the API_TOKEN env var.
func BearerAuth(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		token := extractBearerToken(r)

		if token == "" {
			slog.Warn("missing authorization token", "path", r.URL.Path, "remote", r.RemoteAddr)
			writeJSON(w, http.StatusUnauthorized, errorResponse{Error: "authorization token required"})
			return
		}

		expectedToken := os.Getenv("API_TOKEN")
		if token != expectedToken {
			slog.Warn("invalid authorization token", "path", r.URL.Path, "remote", r.RemoteAddr)
			writeJSON(w, http.StatusUnauthorized, errorResponse{Error: "invalid authorization token"})
			return
		}

		next.ServeHTTP(w, r)
	})
}

func extractBearerToken(r *http.Request) string {
	authHeader := r.Header.Get("Authorization")
	if authHeader == "" {
		return ""
	}
	parts := strings.SplitN(authHeader, " ", 2)
	if len(parts) != 2 || !strings.EqualFold(parts[0], "Bearer") {
		return ""
	}
	return strings.TrimSpace(parts[1])
}

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}
