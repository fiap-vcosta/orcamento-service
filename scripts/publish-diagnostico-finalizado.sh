#!/bin/sh
set -eu

HOST="${PUBSUB_EMULATOR_HOST:-127.0.0.1:8088}"
PROJECT="${PUBSUB_PROJECT_ID:-local-orcamento}"
TOPIC="${TOPIC:-diagnostico-finalizado}"
ORDEM_ID="${ORDEM_ID:-10}"
VERSAO="${VERSAO:-1}"
NOME="${NOME:-APRO}"
EMAIL="${EMAIL:-test_user@test.com}"
DOCUMENTO="${DOCUMENTO:-86421537090}"
EVENT_ID="${EVENT_ID:-$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid)}"
OCCURRED_AT="${OCCURRED_AT:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"

export PUBSUB_EMULATOR_HOST="${HOST}"
export CLOUDSDK_CORE_PROJECT="${PROJECT}"
export CLOUDSDK_AUTH_DISABLE_CREDENTIALS=true

BODY=$(cat <<EOF
{
  "eventId": "${EVENT_ID}",
  "type": "DiagnosticoFinalizado",
  "version": 2,
  "occurredAt": "${OCCURRED_AT}",
  "data": {
    "ordemServicoId": ${ORDEM_ID},
    "versaoDiagnostico": ${VERSAO},
    "cliente": {
      "nome": "${NOME}",
      "email": "${EMAIL}",
      "documento": "${DOCUMENTO}"
    },
    "servicos": [
      {
        "id": 1,
        "nome": "Troca de oleo",
        "valorCobrado": 150.0,
        "status": "Sugerido"
      }
    ]
  }
}
EOF
)

gcloud pubsub topics publish "${TOPIC}" \
  --project="${PROJECT}" \
  --message="${BODY}" \
  --attribute=eventType=DiagnosticoFinalizado

echo "Published DiagnosticoFinalizado eventId=${EVENT_ID} ordemServicoId=${ORDEM_ID} versao=${VERSAO}"
