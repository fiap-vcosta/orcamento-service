# orcamento-service

[![Quality Gate](https://sonarcloud.io/api/project_badges/quality_gate?project=fiap-vcosta_orcamento-service)](https://sonarcloud.io/summary/new_code?id=fiap-vcosta_orcamento-service)
[![Reliability](https://sonarcloud.io/api/project_badges/measure?project=fiap-vcosta_orcamento-service&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=fiap-vcosta_orcamento-service)
[![Security](https://sonarcloud.io/api/project_badges/measure?project=fiap-vcosta_orcamento-service&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=fiap-vcosta_orcamento-service)
[![Maintainability](https://sonarcloud.io/api/project_badges/measure?project=fiap-vcosta_orcamento-service&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=fiap-vcosta_orcamento-service)
[![Duplications](https://sonarcloud.io/api/project_badges/measure?project=fiap-vcosta_orcamento-service&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=fiap-vcosta_orcamento-service)

Serviço de orçamento e cobrança Pix da oficina (Mercado Pago Orders). Gera o orçamento a partir do diagnóstico finalizado, publica o resultado da cobrança e recebe o webhook de pagamento.

Stack: .NET 8, Firestore (database nomeado) e Pub/Sub. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Requisitos: [docs/01_requisitos.md](docs/01_requisitos.md).

## Paths

| Método | Path | Acesso |
|--------|------|--------|
| `GET` | `/orcamento/health` | anônimo |
| `POST` | `/orcamento/webhook/mercado-pago` | anônimo; valida `x-signature` |
| `POST` | `/orcamento/ordens/{ordemServicoId}/cobrancas` | JWT staff; nova Pix após expiração (OS em `CobrancaExpirada`) |

## Local

```bash
cp .env.example .env
./scripts/setup-local-jwt.sh
export NUGET_AUTH_TOKEN=…   # PAT read:packages

# Só emuladores (dotnet run no host)
docker compose up firestore pubsub pubsub-init -d
export FIRESTORE_EMULATOR_HOST=127.0.0.1:8087
export PUBSUB_EMULATOR_HOST=127.0.0.1:8088
dotnet run --project src/Api/Api.csproj
# → http://localhost:8080/orcamento/health

# API + emuladores no Compose (porta 8084)
docker compose --profile app up --build
# → http://localhost:8084/orcamento/health
```

Portas do Compose: API `8084`, Firestore host `8087`, Pub/Sub host `8088`.

Localmente o Firestore usa o database `(default)` do emulador. Na GCP o database nomeado é `orcamento`.

Workers Pub/Sub usam o emulador (`PUBSUB_EMULATOR_HOST`; `building-blocks` ≥ 1.1.1). Prova rápida:

```bash
./scripts/publish-diagnostico-finalizado.sh
```

Webhook assinado (Requestly): manifest `id:{dataId};request-id:{xRequestId};ts:{ts};` com `dataId` em minúsculas; `x-signature` = `ts={ts},v1={hmac_sha256_hex(secret, manifest)}`. Preencha `webhookSecret` e `xSignature` só na UI — valores vazios no JSON commitado.

Requestly: importar [`docs/requestly/orcamento.requestly.json`](docs/requestly/orcamento.requestly.json).

Credenciais de sandbox do Mercado Pago ficam no `.env` local e, na GCP, em org secrets do GitHub (`MERCADO_PAGO_ACCESS_TOKEN`, `MERCADO_PAGO_WEBHOOK_SECRET`) — nunca no Git.

## Testes

```bash
dotnet restore TechChallenge.sln
dotnet build TechChallenge.sln -c Release
dotnet test tests/UnitTests/UnitTests.csproj -c Release \
  "/p:CollectCoverage=true" \
  "/p:CoverletOutputFormat=cobertura" \
  "/p:Include=[Domain]*%2c[Application]*%2c[Infrastructure]*%2c[Api]*" \
  "/p:Threshold=80" \
  "/p:ThresholdType=line%2Cbranch" \
  "/p:ThresholdStat=minimum"
dotnet test tests/IntegrationTests/IntegrationTests.csproj -c Release
```

## CI

Callers dos reusables em [`fiap-vcosta/actions`](https://github.com/fiap-vcosta/actions):

- **dotnet** — InspectCode, unit (gate 80%), integração, SonarCloud (`fiap-vcosta_orcamento-service`)
- **terraform** — `fmt` / `validate` no root `terraform/`

## Terraform (GCP)

Root module: [`terraform/`](terraform/). State GCS prefix `orcamento-service`.

| Recurso | Papel |
|---------|--------|
| Firestore database `orcamento` | Persistência de orçamentos e cobranças |
| Subscriptions + DLQ | `orcamento-diagnostico-finalizado`, `orcamento-ordem-servico-finalizada`, `orcamento-ordem-servico-descartada` |
| Publisher IAM | tópicos de orçamento/cobrança/pagamento + `notificacoes` |
| `orcamento-notificacoes-inspecao` | inspeção no tópico `notificacoes` (retenção 1 dia, sem worker) |

## CD (GCP)

| Workflow | Gatilho | Papel |
|----------|---------|--------|
| `build-push` | push em `main` | Imagem no Artifact Registry |
| `tf-apply` / `tf-destroy` | `workflow_dispatch` | Firestore + Pub/Sub |
| `deploy` | `workflow_dispatch` | Secret k8s (org: MP + DD + service key) + manifests + smoke |

Pré-requisitos do `deploy`:

1. Cluster + Gateway do [`infra-k8s`](https://github.com/fiap-vcosta/infra-k8s) no ar
2. Tópicos do [`infra-events`](https://github.com/fiap-vcosta/infra-events)
3. `tf-apply` deste repo
4. Org secrets `MERCADO_PAGO_ACCESS_TOKEN` e `MERCADO_PAGO_WEBHOOK_SECRET` (antes do Pix na demo)
5. Imagem publicada pelo `build-push`

Smoke: `https://vcosta-fiap.online/orcamento/health`. O `HTTPRoute` expõe `/orcamento` (webhook incluso).
