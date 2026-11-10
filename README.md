# orcamento-service

Serviço de orçamento e cobrança Pix da oficina (Mercado Pago Orders). Gera o orçamento a partir do diagnóstico finalizado, publica o resultado da cobrança e recebe o webhook de pagamento.

Stack: .NET 8, Firestore (database nomeado) e Pub/Sub. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Requisitos: [docs/01_requisitos.md](docs/01_requisitos.md).

## Execução local

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
dotnet run --project src/Api/Api.csproj
```

Restore do pacote `building-blocks` exige PAT `read:packages` (`NUGET_AUTH_TOKEN`) e o `nuget.config` deste repositório.

## Paths

| Método | Path | Acesso |
|--------|------|--------|
| `GET` | `/orcamento/health` | anônimo |
| `POST` | `/orcamento/webhook/mercado-pago` | anônimo; valida `x-signature` |

Swagger: `http://localhost:8080/swagger` (porta padrão do `dotnet run`).
