package validator_test

import (
	"testing"

	"github.com/rafmachine/cpf-api/internal/validator"
)

func TestValidate(t *testing.T) {
	tests := []struct {
		name  string
		cpf   string
		valid bool
	}{
		// Valid CPF
		{
			name:  "valid CPF 529.982.247-25",
			cpf:   "52998224725",
			valid: true,
		},
		{
			name:  "valid CPF 111.444.777-35",
			cpf:   "11144477735",
			valid: true,
		},
		{
			name:  "valid CPF 000.000.001-91",
			cpf:   "00000000191",
			valid: true,
		},

		// All same digits (blacklisted)
		{
			name:  "all zeros",
			cpf:   "00000000000",
			valid: false,
		},
		{
			name:  "all ones 111.111.111-11",
			cpf:   "11111111111",
			valid: false,
		},
		{
			name:  "all nines 999.999.999-99",
			cpf:   "99999999999",
			valid: false,
		},

		// Wrong length
		{
			name:  "too short - 10 digits",
			cpf:   "1234567890",
			valid: false,
		},
		{
			name:  "too long - 12 digits",
			cpf:   "123456789012",
			valid: false,
		},
		{
			name:  "empty string",
			cpf:   "",
			valid: false,
		},

		// Non-numeric characters
		{
			name:  "contains letters",
			cpf:   "1234567890A",
			valid: false,
		},
		{
			name:  "contains dots and dash (formatted)",
			cpf:   "529.982.247-25",
			valid: false, // Validate expects digits only
		},
		{
			name:  "contains spaces",
			cpf:   "529 982 247 25",
			valid: false,
		},
		{
			name:  "contains special characters",
			cpf:   "52998224@25",
			valid: false,
		},

		// Invalid check digits
		{
			name:  "wrong first check digit",
			cpf:   "52998224715",
			valid: false,
		},
		{
			name:  "wrong second check digit",
			cpf:   "52998224726",
			valid: false,
		},
		{
			name:  "both check digits wrong",
			cpf:   "52998224700",
			valid: false,
		},
		{
			name:  "sequential digits — mathematically valid CPF",
			cpf:   "12345678909",
			valid: true,
		},
		{
			name:  "sequential digits with wrong check digits",
			cpf:   "12345678901",
			valid: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			got := validator.Validate(tt.cpf)
			if got != tt.valid {
				t.Errorf("Validate(%q) = %v, want %v", tt.cpf, got, tt.valid)
			}
		})
	}
}

func TestFormatCPF(t *testing.T) {
	tests := []struct {
		input    string
		expected string
	}{
		{"52998224725", "529.982.247-25"},
		{"11144477735", "111.444.777-35"},
		{"00000000191", "000.000.001-91"},
		{"short", "short"}, // passthrough on wrong length
	}
	for _, tt := range tests {
		t.Run(tt.input, func(t *testing.T) {
			got := validator.FormatCPF(tt.input)
			if got != tt.expected {
				t.Errorf("FormatCPF(%q) = %q, want %q", tt.input, got, tt.expected)
			}
		})
	}
}

func TestStripCPF(t *testing.T) {
	tests := []struct {
		input    string
		expected string
	}{
		{"529.982.247-25", "52998224725"},
		{"52998224725", "52998224725"},
		{"111.444.777-35", "11144477735"},
		{"", ""},
	}
	for _, tt := range tests {
		t.Run(tt.input, func(t *testing.T) {
			got := validator.StripCPF(tt.input)
			if got != tt.expected {
				t.Errorf("StripCPF(%q) = %q, want %q", tt.input, got, tt.expected)
			}
		})
	}
}
