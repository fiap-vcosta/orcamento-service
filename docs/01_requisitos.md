# Requisitos — orcamento-service

Sessão de Outubro de 2026. Este serviço gera o orçamento a partir do diagnóstico finalizado e cobra com Pix pela Orders API do Mercado Pago. Persistência em Firestore, num database nomeado. Não usa o database `(default)`. Não é dono de ordem de serviço, estoque, catálogo administrativo nem de emissão de JWT.

Payloads, tópicos e chaves de idempotência: [eventos](https://github.com/fiap-vcosta/e2e/blob/main/docs/eventos.md). O webhook está em [HTTP](https://github.com/fiap-vcosta/e2e/blob/main/docs/contratos-http.md).

Credenciais do Mercado Pago ficam fora do repositório. O ambiente de teste é sandbox. Na order de teste, `payer.first_name` igual a `APRO` aprova sozinho.

## 1. Orçamento

* **RF01:** Toda `DiagnosticoFinalizado` gera ou recalcula o orçamento, inclusive quando não há serviço sugerido. O total é a soma de `valorCobrado` dos serviços cujo status não é `Rejeitado`.
* **RF01.1:** Sucesso publica `OrcamentoGerado` e a notificação `OrcamentoDisponivel`. Falha publica `OrcamentoFalhou` com `motivo` em texto livre.
* **RF01.2:** Cada finalização traz um `versaoDiagnostico` novo. A chave de negócio é `ordemServicoId` + `versaoDiagnostico`.
* **RF01.3:** Se `OrdemServicoDescartada` dessa ordem já foi aplicada, `DiagnosticoFinalizado` não cria orçamento.

## 2. Cobrança Pix

* **RF02:** `OrdemServicoFinalizada` cria a cobrança com `POST /v1/orders` (Pix), header `X-Idempotency-Key` e `external_reference` igual ao `ordemServicoId`. O `cobrancaId` é o id da order no Mercado Pago.
* **RF02.1:** Sucesso publica `CobrancaGerada` e a notificação `CobrancaPixDisponivel`, com o QR e o ticket. Uma nova tentativa usa outro `cobrancaId`.
* **RF03:** `OrdemServicoDescartada` cancela o orçamento se ele existir. Sem orçamento, a mensagem é reconhecida e não chama o Mercado Pago.

## 3. Webhook

* **RF04:** `POST /orcamento/webhook/mercado-pago` é público e anônimo. O serviço responde 200 e, em seguida, valida `x-signature` e consulta `GET /v1/orders/{id}`.
* **RF04.1:** Assinatura inválida não publica evento de pagamento.
* **RF04.2:** Order aprovada publica `PagamentoConfirmado`.
* **RF04.3:** Order recusada ou expirada é cancelada (`/v1/orders/{id}/cancel`) e o serviço publica `PagamentoRecusado`. `motivo` é `recusado` ou `expirado`.

## 4. Notificações e idempotência

* **RF05:** `OrcamentoDisponivel` e `CobrancaPixDisponivel` vão para o tópico `notificacoes`. Esse tópico não tem consumidor de aplicação.
* **RNF01:** Reentrega do mesmo `eventId` não muda estado. A mesma chave de negócio, ainda que com outro `eventId`, também não é aplicada de novo. Uma segunda cobrança da mesma ordem é outra chave, porque o `cobrancaId` muda.
