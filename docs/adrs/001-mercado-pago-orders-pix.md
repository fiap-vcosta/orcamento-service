# ADR 001: Mercado Pago Orders para cobrança Pix

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

Na demo da oficina, após a ordem de serviço finalizada, o cliente paga via Pix. O orçamento já foi gerado a partir do diagnóstico; falta um PSP que emita a cobrança, devolva QR/ticket e notifique aprovação, recusa ou expiração.

O problema a ser resolvido é: **Como cobrar Pix na demo com idempotência, webhook verificável e regeneração clara (automática vs. manual), sem inventar um gateway próprio?**

## 2. Decisão

- **API:** Mercado Pago **Orders** — `POST /v1/orders` com pagamento Pix.
- **Idempotência:** header `X-Idempotency-Key` em cada create.
- **Correlação:** `external_reference` = `ordemServicoId` (Guid). O `cobrancaId` é o id da order no Mercado Pago.
- **QR:** se o create não trouxer `qr_code`, o serviço faz `GET /v1/orders/{id}` antes de falhar.
- **Webhook:** `POST /orcamento/webhook/mercado-pago` público; responde 200 e, em seguida, valida `x-signature` e consulta `GET /v1/orders/{id}` antes de publicar evento de pagamento.
- **Recusado:** cancela a order, publica `PagamentoRecusado` (`motivo` = `recusado`) e **regenera automaticamente** outra cobrança Pix.
- **Expirado:** cancela, publica `PagamentoRecusado` (`motivo` = `expirado`) e **não** regenera — staff cria nova via `POST /orcamento/ordens/{ordemServicoId}/cobrancas` (JWT).
- **Secrets:** org secrets `MERCADO_PAGO_ACCESS_TOKEN` e `MERCADO_PAGO_WEBHOOK_SECRET` (sandbox na demo; nunca no Git).
- **Local / e2e:** WireMock fakes a Orders API; Requestly assina o webhook com o secret local.

## 3. Justificativa

* Orders API concentra create, GET e cancel num contrato único alinhado aos requisitos de Outubro/2026.
* `external_reference` + Guid da OS amarra cobrança à ordem sem inventar correlator paralelo.
* Validar assinatura **e** reler a order no webhook evita confiar só no payload notificado.
* Recusado → regeneração automática; expirado → regeneração manual espelha a política de status na OS (`CobrancaExpirada` → nova cobrança staff).

## 4. Alternativas Consideradas

* **Checkout Pro / Preference:** rejeitado — fluxo de redirect/checkout pesado demais para a demo; o requisito pede Pix via Orders.
* **Payments API antiga:** rejeitado — o canon da sessão de Outubro fecha em `POST /v1/orders`.
* **Mock-only (sem PSP real):** rejeitado para a janela GCP — sandbox MP com WireMock só no e2e/local; na demo usa token sandbox real via org secrets.

## 5. Consequências

### Positivas
* Contrato HTTP e eventos já documentados no e2e (`contratos-http`, `eventos`).
* Idempotência no create e chave de negócio por `cobrancaId` nas reentregas.
* Separação clara automática vs. manual na regeneração.

### Negativas / Riscos
* Dependência do sandbox Mercado Pago e dos org secrets na janela GCP.
  * *Mitigação:* WireMock no compose/e2e; secrets fora do Git; `tf-apply`/`deploy` manuais.
* Assinatura inválida ou GET falho não publica pagamento — operador precisa reprocessar/notificar de novo.
  * *Mitigação:* comportamento explícito nos requisitos (RF04.1); logs/monitores Datadog.
