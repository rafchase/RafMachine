package middleware_test

import (
	"net/http"
	"net/http/httptest"
	"os"
	"testing"

	"github.com/rafmachine/cpf-api/internal/middleware"
)

const testToken = "test-secret-token"

// nextHandler is a simple handler that writes 200 OK to confirm it was reached.
var nextHandler = http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
})

func setupToken(t *testing.T) {
	t.Helper()
	t.Setenv("API_TOKEN", testToken)
}

func TestBearerAuth_ValidToken(t *testing.T) {
	setupToken(t)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	req.Header.Set("Authorization", "Bearer "+testToken)
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("expected 200, got %d", rr.Code)
	}
}

func TestBearerAuth_MissingToken(t *testing.T) {
	setupToken(t)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	// No Authorization header
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Errorf("expected 401, got %d", rr.Code)
	}
}

func TestBearerAuth_InvalidToken(t *testing.T) {
	setupToken(t)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	req.Header.Set("Authorization", "Bearer wrong-token")
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Errorf("expected 401, got %d", rr.Code)
	}
}

func TestBearerAuth_EmptyBearerValue(t *testing.T) {
	setupToken(t)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	req.Header.Set("Authorization", "Bearer ")
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Errorf("expected 401, got %d", rr.Code)
	}
}

func TestBearerAuth_WrongScheme(t *testing.T) {
	setupToken(t)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	req.Header.Set("Authorization", "Basic "+testToken)
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Errorf("expected 401, got %d", rr.Code)
	}
}

func TestBearerAuth_NoEnvToken(t *testing.T) {
	// Ensure API_TOKEN is empty
	prev := os.Getenv("API_TOKEN")
	os.Setenv("API_TOKEN", "")
	defer os.Setenv("API_TOKEN", prev)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/12345678909", nil)
	req.Header.Set("Authorization", "Bearer "+testToken)
	rr := httptest.NewRecorder()

	middleware.BearerAuth(nextHandler).ServeHTTP(rr, req)

	// Token won't match empty env var
	if rr.Code != http.StatusUnauthorized {
		t.Errorf("expected 401, got %d", rr.Code)
	}
}
