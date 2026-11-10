#!/bin/sh
set -eu

HOST="${PUBSUB_EMULATOR_HOST:-127.0.0.1:8088}"
PROJECT="${PUBSUB_PROJECT_ID:-local-orcamento}"
export PUBSUB_EMULATOR_HOST="${HOST}"
export CLOUDSDK_CORE_PROJECT="${PROJECT}"
export CLOUDSDK_AUTH_DISABLE_CREDENTIALS=true

echo "Waiting for Pub/Sub emulator at ${HOST}..."
i=0
while [ "$i" -lt 60 ]; do
  if gcloud pubsub topics list --project="${PROJECT}" --format='value(name)' >/dev/null 2>&1; then
    break
  fi
  i=$((i + 1))
  sleep 1
done
gcloud pubsub topics list --project="${PROJECT}" --format='value(name)' >/dev/null

create_topic() {
  topic="$1"
  if gcloud pubsub topics describe "${topic}" --project="${PROJECT}" >/dev/null 2>&1; then
    return 0
  fi
  gcloud pubsub topics create "${topic}" --project="${PROJECT}"
}

create_subscription() {
  subscription="$1"
  topic="$2"
  if gcloud pubsub subscriptions describe "${subscription}" --project="${PROJECT}" >/dev/null 2>&1; then
    return 0
  fi
  gcloud pubsub subscriptions create "${subscription}" \
    --topic="${topic}" \
    --project="${PROJECT}"
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
