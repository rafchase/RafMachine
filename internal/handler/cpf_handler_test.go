package handler_test

import (
	"context"
	"encoding/json"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/go-chi/chi/v5"

	"github.com/rafmachine/cpf-api/internal/handler"
	"github.com/rafmachine/cpf-api/internal/service"
)

// mockCPFService is a test double for service.CPFService.
type mockCPFService struct {
	data *service.CPFData
	err  error
}

func (m *mockCPFService) Consultar(_ context.Context, _ string) (*service.CPFData, error) {
	return m.data, m.err
}

// makeRequest builds an httptest request routed through chi so URL params work.
func makeRequest(t *testing.T, svc service.CPFService, cpf string) *httptest.ResponseRecorder {
	t.Helper()

	h := handler.NewCPFHandler(svc)
	r := chi.NewRouter()
	r.Get("/consulta/cpf/{cpf}", h.Consultar)

	req := httptest.NewRequest(http.MethodGet, "/consulta/cpf/"+cpf, nil)
	rr := httptest.NewRecorder()
	r.ServeHTTP(rr, req)
	return rr
}

// ── Success ────────────────────────────────────────────────────────────────────

func TestCPFHandler_Success(t *testing.T) {
	svc := &mockCPFService{
		data: &service.CPFData{
			CPF:               "529.982.247-25",
			Nome:              "JOAO DA SILVA",
			Situacao:          "REGULAR",
			DataNascimento:    "01/01/1990",
			DigitoVerificador: "25",
		},
	}

	rr := makeRequest(t, svc, "52998224725")

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}

	var resp service.CPFData
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decoding response: %v", err)
	}
	if resp.CPF != "529.982.247-25" {
		t.Errorf("CPF = %q, want %q", resp.CPF, "529.982.247-25")
	}
	if resp.Nome != "JOAO DA SILVA" {
		t.Errorf("Nome = %q, want %q", resp.Nome, "JOAO DA SILVA")
	}
}

// ── Invalid CPF ────────────────────────────────────────────────────────────────

func TestCPFHandler_InvalidCPF_BadFormat(t *testing.T) {
	svc := &mockCPFService{} // should never be called

	rr := makeRequest(t, svc, "1234")

	if rr.Code != http.StatusBadRequest {
		t.Errorf("expected 400, got %d", rr.Code)
	}
}

func TestCPFHandler_InvalidCPF_AllSameDigits(t *testing.T) {
	svc := &mockCPFService{}

	rr := makeRequest(t, svc, "11111111111")

	if rr.Code != http.StatusBadRequest {
		t.Errorf("expected 400, got %d", rr.Code)
	}
}

func TestCPFHandler_InvalidCPF_WrongCheckDigit(t *testing.T) {
	svc := &mockCPFService{}

	rr := makeRequest(t, svc, "52998224700")

	if rr.Code != http.StatusBadRequest {
		t.Errorf("expected 400, got %d", rr.Code)
	}
}

// ── CPF Not Found ──────────────────────────────────────────────────────────────

func TestCPFHandler_NotFound(t *testing.T) {
	svc := &mockCPFService{err: service.ErrNotFound}

	rr := makeRequest(t, svc, "52998224725")

	if rr.Code != http.StatusNotFound {
		t.Errorf("expected 404, got %d", rr.Code)
	}
}

// ── External API Unavailable ───────────────────────────────────────────────────

func TestCPFHandler_ExternalAPIUnavailable(t *testing.T) {
	svc := &mockCPFService{err: service.ErrUnavailable}

	rr := makeRequest(t, svc, "52998224725")

	if rr.Code != http.StatusServiceUnavailable {
		t.Errorf("expected 503, got %d", rr.Code)
	}
}

// ── Unexpected Error ───────────────────────────────────────────────────────────

func TestCPFHandler_InternalError(t *testing.T) {
	svc := &mockCPFService{err: errors.New("unexpected db error")}

	rr := makeRequest(t, svc, "52998224725")

	if rr.Code != http.StatusInternalServerError {
		t.Errorf("expected 500, got %d", rr.Code)
	}
}

// ── Content-Type ───────────────────────────────────────────────────────────────

func TestCPFHandler_ResponseContentType(t *testing.T) {
	svc := &mockCPFService{
		data: &service.CPFData{
			CPF:               "529.982.247-25",
			Nome:              "TEST",
			Situacao:          "REGULAR",
			DataNascimento:    "01/01/2000",
			DigitoVerificador: "25",
		},
	}

	rr := makeRequest(t, svc, "52998224725")

	ct := rr.Header().Get("Content-Type")
	if ct != "application/json" {
		t.Errorf("Content-Type = %q, want application/json", ct)
	}
}

// ── Formatted CPF in path ──────────────────────────────────────────────────────

func TestCPFHandler_FormattedCPFInPath(t *testing.T) {
	svc := &mockCPFService{
		data: &service.CPFData{
			CPF:               "529.982.247-25",
			Nome:              "JOAO",
			Situacao:          "REGULAR",
			DataNascimento:    "01/01/1990",
			DigitoVerificador: "25",
		},
	}

	// Pass formatted CPF — handler should strip and validate
	rr := makeRequest(t, svc, "529.982.247-25")

	if rr.Code != http.StatusOK {
		t.Errorf("expected 200, got %d", rr.Code)
	}
}
