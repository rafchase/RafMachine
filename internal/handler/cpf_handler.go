package handler

import (
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"regexp"

	"github.com/go-chi/chi/v5"
	"github.com/rafmachine/cpf-api/internal/service"
	"github.com/rafmachine/cpf-api/internal/validator"
)

var onlyDigits = regexp.MustCompile(`^\d+$`)

type errorResponse struct {
	Error string `json:"error"`
}

// CPFHandler handles CPF lookup requests.
type CPFHandler struct {
	svc service.CPFService
}

// NewCPFHandler creates a new CPFHandler.
func NewCPFHandler(svc service.CPFService) *CPFHandler {
	return &CPFHandler{svc: svc}
}

// Consultar handles GET /consulta/cpf/{cpf}.
func (h *CPFHandler) Consultar(w http.ResponseWriter, r *http.Request) {
	rawCPF := chi.URLParam(r, "cpf")

	// Strip formatting (dots and dashes) before validation
	cpf := validator.StripCPF(rawCPF)

	slog.Info("CPF consultation request", "cpf", maskCPF(cpf), "remote", r.RemoteAddr)

	if !validator.Validate(cpf) {
		slog.Warn("invalid CPF format", "cpf", maskCPF(cpf))
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "invalid CPF: must be 11 digits with valid check digits"})
		return
	}

	data, err := h.svc.Consultar(r.Context(), cpf)
	if err != nil {
		switch {
		case errors.Is(err, service.ErrNotFound):
			slog.Warn("CPF not found", "cpf", maskCPF(cpf))
			writeJSON(w, http.StatusNotFound, errorResponse{Error: "CPF not found"})
		case errors.Is(err, service.ErrUnavailable):
			slog.Error("external API unavailable", "cpf", maskCPF(cpf), "error", err)
			writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "external CPF service is currently unavailable"})
		default:
			slog.Error("unexpected error consulting CPF", "cpf", maskCPF(cpf), "error", err)
			writeJSON(w, http.StatusInternalServerError, errorResponse{Error: "internal server error"})
		}
		return
	}

	slog.Info("CPF consultation success", "cpf", maskCPF(cpf))
	writeJSON(w, http.StatusOK, data)
}

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}

// maskCPF masks the middle digits for safe logging.
func maskCPF(cpf string) string {
	if len(cpf) != 11 {
		return "***"
	}
	return cpf[0:3] + ".***.***-" + cpf[9:11]
}
