package validator

import (
	"regexp"
	"strconv"
)

var onlyDigits = regexp.MustCompile(`^\d{11}$`)

// Validate checks whether a CPF string (digits only, 11 chars) is valid.
// It rejects CPFs with all equal digits and verifies both check digits.
func Validate(cpf string) bool {
	if !onlyDigits.MatchString(cpf) {
		return false
	}

	// Reject CPFs with all identical digits (e.g. 11111111111)
	allSame := true
	for i := 1; i < 11; i++ {
		if cpf[i] != cpf[0] {
			allSame = false
			break
		}
	}
	if allSame {
		return false
	}

	digits := make([]int, 11)
	for i, ch := range cpf {
		d, err := strconv.Atoi(string(ch))
		if err != nil {
			return false
		}
		digits[i] = d
	}

	// Validate first check digit
	sum := 0
	for i := 0; i < 9; i++ {
		sum += digits[i] * (10 - i)
	}
	remainder := (sum * 10) % 11
	if remainder == 10 {
		remainder = 0
	}
	if remainder != digits[9] {
		return false
	}

	// Validate second check digit
	sum = 0
	for i := 0; i < 10; i++ {
		sum += digits[i] * (11 - i)
	}
	remainder = (sum * 10) % 11
	if remainder == 10 {
		remainder = 0
	}
	if remainder != digits[10] {
		return false
	}

	return true
}

// FormatCPF formats an 11-digit CPF string as "000.000.000-00".
func FormatCPF(cpf string) string {
	if len(cpf) != 11 {
		return cpf
	}
	return cpf[0:3] + "." + cpf[3:6] + "." + cpf[6:9] + "-" + cpf[9:11]
}

// StripCPF removes non-digit characters from a CPF string.
func StripCPF(cpf string) string {
	re := regexp.MustCompile(`\D`)
	return re.ReplaceAllString(cpf, "")
}
