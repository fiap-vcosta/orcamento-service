# orcamento-service

Serviço de orçamento e cobrança Pix da oficina (Mercado Pago Orders). Gera o orçamento a partir do diagnóstico finalizado, publica o resultado da cobrança e recebe o webhook de pagamento.

Stack: .NET 8, Firestore (database nomeado) e Pub/Sub. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Requisitos: [docs/01_requisitos.md](docs/01_requisitos.md).

## Paths

| Método | Path | Acesso |
|--------|------|--------|
| `GET` | `/orcamento/health` | anônimo |
| `POST` | `/orcamento/webhook/mercado-pago` | anônimo; valida `x-signature` |

## Local

```bash
cp .env.example .env
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

Prova rápida de orçamento (sem Mercado Pago):

```bash
./scripts/publish-diagnostico-finalizado.sh
# logs da API / pull na subscription de inspeção → OrcamentoGerado
```

Webhook assinado (Requestly): manifest `id:{dataId};request-id:{xRequestId};ts:{ts};` com `dataId` em minúsculas; `x-signature` = `ts={ts},v1={hmac_sha256_hex(secret, manifest)}`. Preencha `webhookSecret` e `xSignature` só na UI — valores vazios no JSON commitado.

```bash
# exemplo (secret local de teste)
DATA_ID=ord_test
REQUEST_ID=req-local-1
TS=1742505638683
SECRET='seu-segredo'
MANIFEST="id:${DATA_ID};request-id:${REQUEST_ID};ts:${TS};"
V1=$(printf '%s' "$MANIFEST" | openssl dgst -sha256 -hmac "$SECRET" | awk '{print $2}')
echo "ts=${TS},v1=${V1}"
```

Requestly: importar [`docs/requestly/orcamento.requestly.json`](docs/requestly/orcamento.requestly.json).

Credenciais de sandbox do Mercado Pago (`MERCADO_PAGO_ACCESS_TOKEN` / `MERCADO_PAGO_WEBHOOK_SECRET`) ficam no `.env` — nunca no Git. Cobrança Pix e webhook real só fazem sentido com o token preenchido.

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

Restore do pacote `building-blocks` exige PAT `read:packages` (`NUGET_AUTH_TOKEN`) e o `nuget.config` deste repositório.

Swagger: `http://localhost:8080/swagger` (`dotnet run`) ou `http://localhost:8084/swagger` (Compose).
