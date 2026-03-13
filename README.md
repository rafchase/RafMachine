# CPF API

API REST em Go para consulta de dados de CPF na Receita Federal, com autenticação via Bearer Token.

## Tecnologias Utilizadas

| Tecnologia | Versão | Uso |
|---|---|---|
| Go | 1.21+ | Linguagem principal |
| [chi](https://github.com/go-chi/chi) | v5 | Roteamento HTTP |
| [godotenv](https://github.com/joho/godotenv) | v1.5 | Carregamento de variáveis de ambiente |
| Docker | 24+ | Containerização |
| Alpine Linux | 3.19 | Imagem base (< 20MB) |

## Quickstart com Docker

```bash
# 1. Configurar variáveis de ambiente
cp .env.example .env && echo "Edite .env e defina API_TOKEN"

# 2. Build e inicialização
docker compose up --build -d

# 3. Testar
curl -H "Authorization: Bearer <seu-token>" \
  http://localhost:8080/consulta/cpf/52998224725
```

## Estrutura do Projeto

```
.
├── cmd/api/main.go                    # Entry point
├── internal/
│   ├── handler/cpf_handler.go         # Handler HTTP
│   ├── middleware/auth.go             # Autenticação Bearer
│   ├── service/cpf_service.go         # Integração com API externa
│   └── validator/cpf_validator.go     # Validação de CPF
├── docs/
│   ├── API.md                         # Documentação da API
│   └── TESTES.md                      # Documentação de Testes
├── Dockerfile                         # Multi-stage build
├── docker-compose.yml
├── .env.example
└── go.mod
```

## Documentação

- [Documentação da API](docs/API.md) — endpoints, exemplos curl/Postman, tabela de erros
- [Documentação de Testes](docs/TESTES.md) — casos de teste, cobertura, relatório HTML

## Rodando os Testes

```bash
go test ./... -cover
```

## Variáveis de Ambiente

| Variável | Obrigatória | Padrão | Descrição |
|---|---|---|---|
| `API_TOKEN` | Sim | — | Bearer Token para autenticação |
| `PORT` | Não | `8080` | Porta HTTP |
| `RECEITA_WS_URL` | Não | `https://www.receitaws.com.br/v1/cpf` | URL da API externa |
