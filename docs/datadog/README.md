# Datadog — `orcamento-service`

Site: `us5.datadoghq.com`. Dashboard geral da Saga: repo `e2e` → `docs/datadog/`.

## Arquivos

| Arquivo | Uso |
|---------|-----|
| [`dashboard-service-demo.json`](dashboard-service-demo.json) | APM/logs deste serviço (`DD_SERVICE=orcamento-service`) |
| [`monitor-erros-http.json`](monitor-erros-http.json) | Erros APM HTTP |
| [`monitor-health-uptime.json`](monitor-health-uptime.json) | Hits em `GET /orcamento/health` (`notify_no_data: false`) |

## Importar

1. Abra [https://us5.datadoghq.com](https://us5.datadoghq.com).
2. **Dashboards** → **Import dashboard JSON** → [`dashboard-service-demo.json`](dashboard-service-demo.json).
3. **Monitors** → importe cada `monitor-*.json`. Destino de alerta só na UI (não versionar e-mail).
4. Após `demo-down`, mute os monitores.

## Links úteis

| Recurso | URL |
|---------|-----|
| APM — `orcamento-service` | https://us5.datadoghq.com/apm/entity/service%3Aorcamento-service |
| Health (GCP) | https://vcosta-fiap.online/orcamento/health |

Logs JSON com `DD_LOGS_INJECTION=true` levam `dd.trace_id` → **View Trace**.

## Grupos do dashboard

Cores por tipo (só grupos com conteúdo): **Monitores** (purple), **APIs / golden metrics** (vivid_blue), **Aplicação** (blue), **Eventos de entrada** (orange), **Eventos de saída** (green), **Dead-letter** (pink), **Banco** (gray — Cloud SQL ou Firestore), **Insights de negócio** (yellow).
O grupo Monitores usa `manage_status` com `name:[orcamento]*`. Grupos sem dados (ex.: Pub/Sub/DLQ/negócio) são omitidos.

