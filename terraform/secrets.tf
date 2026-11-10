resource "google_secret_manager_secret" "mercado_pago_access_token" {
  secret_id = "orcamento-service-mercado-pago-access-token"
  project   = var.project_id

  labels = {
    service = "orcamento-service"
  }

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret" "mercado_pago_webhook_secret" {
  secret_id = "orcamento-service-mercado-pago-webhook-secret"
  project   = var.project_id

  labels = {
    service = "orcamento-service"
  }

  replication {
    auto {}
  }
}
