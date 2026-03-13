# API de Consulta de CPF

## Visão Geral

API REST em Go para consulta de CPF com autenticação via Bearer Token. A API valida o CPF localmente (formato e dígitos verificadores) antes de consultar a API pública da Receita WS.

## Pré-requisitos

- Go 1.21+
- Docker e Docker Compose (para execução containerizada)
- Conta e token de acesso próprio (variável `API_TOKEN`)

## Instalação

```bash
git clone <repositório>
cd RafMachine

# Copiar e configurar variáveis de ambiente
cp .env.example .env
# Edite .env e defina API_TOKEN com um valor seguro
```

## Como Rodar Localmente

```bash
# Instalar dependências
go mod download

# Rodar o servidor
go run ./cmd/api
```

O servidor inicia na porta definida em `PORT` (padrão: `8080`).

## Como Rodar via Docker

```bash
# Build e start
docker compose up --build -d

# Verificar status
docker compose ps

# Ver logs
docker compose logs -f api

# Parar
docker compose down
```

---

## Endpoint

### `GET /consulta/cpf/{cpf}`

Consulta os dados de um CPF na Receita Federal.

**Autenticação:** Bearer Token obrigatório no header `Authorization`.

#### Parâmetros de Path

| Parâmetro | Tipo   | Descrição                                                    |
|-----------|--------|--------------------------------------------------------------|
| `cpf`     | string | CPF com 11 dígitos numéricos (aceita formato `000.000.000-00`) |

---

## Exemplos de Requisição e Resposta

### Sucesso — 200 OK

**Request:**
```http
GET /consulta/cpf/52998224725 HTTP/1.1
Host: localhost:8080
Authorization: Bearer supersecret123
```

**Response:**
```json
{
  "cpf": "529.982.247-25",
  "nome": "JOAO DA SILVA",
  "situacao": "REGULAR",
  "data_nascimento": "01/01/1990",
  "digito_verificador": "25"
}
```

Também aceita CPF formatado na URL:
```bash
curl -H "Authorization: Bearer supersecret123" \
  http://localhost:8080/consulta/cpf/529.982.247-25
```

---

### CPF Inválido — 400 Bad Request

**Request:**
```http
GET /consulta/cpf/12345 HTTP/1.1
Authorization: Bearer supersecret123
```

**Response:**
```json
{
  "error": "invalid CPF: must be 11 digits with valid check digits"
}
```

Casos que geram 400:
- Menos ou mais de 11 dígitos
- Todos os dígitos iguais (ex: `11111111111`)
- Dígitos verificadores inválidos
- Caracteres não numéricos (após remoção de `.` e `-`)

---

### Não Autorizado — 401 Unauthorized

**Request sem token:**
```http
GET /consulta/cpf/52998224725 HTTP/1.1
```

**Response:**
```json
{
  "error": "authorization token required"
}
```

**Request com token inválido:**
```http
GET /consulta/cpf/52998224725 HTTP/1.1
Authorization: Bearer token-errado
```

**Response:**
```json
{
  "error": "invalid authorization token"
}
```

---

### CPF Não Encontrado — 404 Not Found

**Response:**
```json
{
  "error": "CPF not found"
}
```

---

### Serviço Externo Indisponível — 503 Service Unavailable

**Response:**
```json
{
  "error": "external CPF service is currently unavailable"
}
```

Retornado quando a API externa está fora do ar ou o timeout de 10s é atingido.

---

### Erro Interno — 500 Internal Server Error

**Response:**
```json
{
  "error": "internal server error"
}
```

---

### Health Check — 200 OK (sem autenticação)

```http
GET /health HTTP/1.1
```

**Response:**
```json
{"status":"ok"}
```

---

## Tabela de Códigos de Erro

| HTTP Status | Código        | Situação                                         |
|-------------|---------------|--------------------------------------------------|
| 200         | OK            | Consulta realizada com sucesso                   |
| 400         | Bad Request   | CPF com formato inválido ou dígitos incorretos   |
| 401         | Unauthorized  | Token ausente ou inválido                        |
| 404         | Not Found     | CPF não encontrado na base da Receita            |
| 503         | Unavailable   | API externa indisponível ou timeout              |
| 500         | Server Error  | Erro interno inesperado                          |

---

## Exemplos com curl

```bash
# Consulta com CPF em dígitos puros
curl -s \
  -H "Authorization: Bearer supersecret123" \
  http://localhost:8080/consulta/cpf/52998224725 | jq .

# Consulta com CPF formatado
curl -s \
  -H "Authorization: Bearer supersecret123" \
  "http://localhost:8080/consulta/cpf/529.982.247-25" | jq .

# Sem token (deve retornar 401)
curl -s http://localhost:8080/consulta/cpf/52998224725 | jq .

# CPF inválido (deve retornar 400)
curl -s \
  -H "Authorization: Bearer supersecret123" \
  http://localhost:8080/consulta/cpf/11111111111 | jq .

# Health check
curl -s http://localhost:8080/health
```

---

## Exemplos com Postman

### Configurar Collection

1. Crie uma nova Collection chamada **CPF API**.
2. Adicione uma variável de collection: `base_url = http://localhost:8080` e `token = supersecret123`.

### Consultar CPF (sucesso)

- **Method:** GET
- **URL:** `{{base_url}}/consulta/cpf/52998224725`
- **Headers:**
  - `Authorization: Bearer {{token}}`

### Testar 401

- **Method:** GET
- **URL:** `{{base_url}}/consulta/cpf/52998224725`
- *(sem header Authorization)*

### Testar 400

- **Method:** GET
- **URL:** `{{base_url}}/consulta/cpf/11111111111`
- **Headers:**
  - `Authorization: Bearer {{token}}`

---

## Variáveis de Ambiente

| Variável        | Obrigatória | Padrão                                    | Descrição                          |
|-----------------|-------------|-------------------------------------------|------------------------------------|
| `API_TOKEN`     | Sim         | —                                         | Token Bearer para autenticação     |
| `PORT`          | Não         | `8080`                                    | Porta do servidor HTTP             |
| `RECEITA_WS_URL`| Não         | `https://www.receitaws.com.br/v1/cpf`     | URL base da API externa de CPF     |
