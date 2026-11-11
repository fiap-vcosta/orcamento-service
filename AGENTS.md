# Tech Challenge — Guia para agentes (`orcamento-service`)

Serviço de orçamento e cobrança Pix (Mercado Pago Orders). Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Ler o README deste repositório e [`docs/01_requisitos.md`](docs/01_requisitos.md)
2. Não inventar features fora do escopo descrito nas rules
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout

| Peça | Papel |
|------|--------|
| `src/Domain` | Orçamento, cobrança, ordem descartada |
| `src/Application` | Saga MediatR + consumers Pub/Sub |
| `src/Infrastructure` | Firestore, Mercado Pago Orders, publisher |
| `src/Api` | Webhook `/orcamento/webhook/mercado-pago`, staff `POST /orcamento/ordens/{id}/cobrancas`, health |
| `tests/UnitTests` | Gate Coverlet 80% |
| `tests/IntegrationTests` | HTTP + fakes |
| `terraform/` | Firestore nomeado + subscriptions/DLQ |
| `k8s/` | Deployment (Datadog), Service, HPA, KSA, HTTPRoute `/orcamento` |

## Fora de escopo

Ordem de serviço, estoque, catálogo administrativo e emissão de JWT.

## Comandos

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
docker compose --profile app up --build
cd terraform && terraform fmt -check && terraform init -backend=false && terraform validate
```
