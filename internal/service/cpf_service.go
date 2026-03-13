package service

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"time"
)

// ErrNotFound is returned when the CPF is not found in the external API.
var ErrNotFound = errors.New("CPF not found")

// ErrUnavailable is returned when the external API is unreachable or times out.
var ErrUnavailable = errors.New("external API unavailable")

// CPFData holds the response data returned to clients.
type CPFData struct {
	CPF              string `json:"cpf"`
	Nome             string `json:"nome"`
	Situacao         string `json:"situacao"`
	DataNascimento   string `json:"data_nascimento"`
	DigitoVerificador string `json:"digito_verificador"`
}

// ReceitaWSResponse mirrors the external API response shape.
type ReceitaWSResponse struct {
	Status         string `json:"status"`
	Nome           string `json:"nome"`
	Situacao       string `json:"situacao"`
	DataNascimento string `json:"data_nascimento,omitempty"`
	DigitoVerificador string `json:"digito_verificador,omitempty"`
	Mensagem       string `json:"message,omitempty"`
}

// CPFService defines the contract for CPF lookup.
type CPFService interface {
	Consultar(ctx context.Context, cpf string) (*CPFData, error)
}

type cpfService struct {
	httpClient *http.Client
	baseURL    string
}

// NewCPFService creates a new CPFService with a 10-second timeout HTTP client.
func NewCPFService(baseURL string) CPFService {
	return &cpfService{
		httpClient: &http.Client{Timeout: 10 * time.Second},
		baseURL:    baseURL,
	}
}

// NewCPFServiceWithClient creates a new CPFService with a custom HTTP client (useful for testing).
func NewCPFServiceWithClient(baseURL string, client *http.Client) CPFService {
	return &cpfService{
		httpClient: client,
		baseURL:    baseURL,
	}
}

func (s *cpfService) Consultar(ctx context.Context, cpf string) (*CPFData, error) {
	url := fmt.Sprintf("%s/%s", s.baseURL, cpf)

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return nil, fmt.Errorf("creating request: %w", err)
	}
	req.Header.Set("Accept", "application/json")

	resp, err := s.httpClient.Do(req)
	if err != nil {
		if errors.Is(err, context.DeadlineExceeded) || isTimeoutError(err) {
			return nil, ErrUnavailable
		}
		return nil, ErrUnavailable
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusNotFound {
		return nil, ErrNotFound
	}

	if resp.StatusCode != http.StatusOK {
		return nil, ErrUnavailable
	}

	var external ReceitaWSResponse
	if err := json.NewDecoder(resp.Body).Decode(&external); err != nil {
		return nil, fmt.Errorf("decoding response: %w", err)
	}

	if external.Status == "ERROR" {
		return nil, ErrNotFound
	}

	// Format CPF as "000.000.000-00"
	formattedCPF := formatCPF(cpf)

	// Extract check digits (last 2 digits)
	dv := ""
	if len(cpf) == 11 {
		dv = cpf[9:11]
	}

	return &CPFData{
		CPF:              formattedCPF,
		Nome:             external.Nome,
		Situacao:         external.Situacao,
		DataNascimento:   external.DataNascimento,
		DigitoVerificador: dv,
	}, nil
}

func formatCPF(cpf string) string {
	if len(cpf) != 11 {
		return cpf
	}
	return cpf[0:3] + "." + cpf[3:6] + "." + cpf[6:9] + "-" + cpf[9:11]
}

func isTimeoutError(err error) bool {
	if err == nil {
		return false
	}
	type timeoutErr interface {
		Timeout() bool
	}
	var te timeoutErr
	if errors.As(err, &te) {
		return te.Timeout()
	}
	return false
}
