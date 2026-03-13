# ── Stage 1: Builder ──────────────────────────────────────────────────────────
FROM golang:1.21-alpine AS builder

# Install build dependencies
RUN apk add --no-cache git ca-certificates tzdata

WORKDIR /app

# Cache dependency download layer
COPY go.mod go.sum ./
RUN go mod download

# Copy source and build
COPY . .
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -ldflags="-w -s" -o /app/api ./cmd/api

# ── Stage 2: Final image ───────────────────────────────────────────────────────
FROM alpine:3.19

# ca-certificates needed for HTTPS calls to external APIs
RUN apk --no-cache add ca-certificates tzdata

WORKDIR /app

COPY --from=builder /app/api .

# Non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/api"]
