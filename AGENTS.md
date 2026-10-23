# Tech Challenge — Guia para agentes (`orcamento-service`)

Serviço de orçamento e cobrança Pix (Mercado Pago Orders). Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Ler o README deste repositório e, quando existirem, ADRs em `docs/adrs/`
2. Não inventar features fora do escopo descrito nas rules
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout alvo

Quando o código chegar: `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`, `tests/` espelhando `src/`, `k8s/`, `terraform/`, `Dockerfile`, `docker-compose.yml`. Dependências apontam para dentro.

## Fora de escopo

Ordem de serviço, estoque, catálogo administrativo e emissão de JWT.

## Comandos

Ainda não há solution, testes nem scripts neste repositório. Não inventar comandos `dotnet` até o código existir.
