#!/bin/sh
set -eu

HOST="${PUBSUB_EMULATOR_HOST:-127.0.0.1:8088}"
PROJECT="${PUBSUB_PROJECT_ID:-local-orcamento}"
BASE="http://${HOST}/v1/projects/${PROJECT}"

echo "Waiting for Pub/Sub emulator at ${HOST}..."
i=0
while [ "$i" -lt 60 ]; do
  if curl -fsS "${BASE}/topics" >/dev/null 2>&1; then
    break
  fi
  i=$((i + 1))
  sleep 1
done
curl -fsS "${BASE}/topics" >/dev/null

create_topic() {
  topic="$1"
  code="$(curl -sS -o /dev/null -w '%{http_code}' -X PUT "${BASE}/topics/${topic}" \
    -H 'Content-Type: application/json' \
    -d '{}')"
  case "$code" in
    200|409) ;;
    *)
      echo "Failed to create topic ${topic}: HTTP ${code}" >&2
      exit 1
      ;;
  esac
}

create_subscription() {
  subscription="$1"
  topic="$2"
  code="$(curl -sS -o /dev/null -w '%{http_code}' -X PUT "${BASE}/subscriptions/${subscription}" \
    -H 'Content-Type: application/json' \
    -d "{\"topic\":\"projects/${PROJECT}/topics/${topic}\"}")"
  case "$code" in
    200|409) ;;
    *)
      echo "Failed to create subscription ${subscription}: HTTP ${code}" >&2
      exit 1
      ;;
  esac
}

for topic in \
  diagnostico-finalizado \
  orcamento-gerado \
  orcamento-falhou \
  ordem-servico-finalizada \
  cobranca-gerada \
  pagamento-confirmado \
  pagamento-recusado \
  ordem-servico-descartada \
  notificacoes
do
  create_topic "$topic"
done

create_subscription orcamento-diagnostico-finalizado diagnostico-finalizado
create_subscription orcamento-ordem-servico-finalizada ordem-servico-finalizada
create_subscription orcamento-ordem-servico-descartada ordem-servico-descartada
create_subscription orcamento-notificacoes-inspecao notificacoes

echo "Pub/Sub emulator topics and subscriptions ready."
