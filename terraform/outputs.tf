output "firestore_database_id" {
  description = "Id do database Firestore nomeado deste serviço."
  value       = google_firestore_database.orcamento.name
}

output "mercado_pago_access_token_secret_id" {
  description = "Secret Manager id do access token do Mercado Pago (sem versão até o humano adicionar)."
  value       = google_secret_manager_secret.mercado_pago_access_token.secret_id
}

output "mercado_pago_webhook_secret_secret_id" {
  description = "Secret Manager id do segredo de assinatura do webhook Mercado Pago (sem versão até o humano adicionar)."
  value       = google_secret_manager_secret.mercado_pago_webhook_secret.secret_id
}

output "subscription_names" {
  description = "Subscriptions pull consumidas pelo serviço."
  value       = [for s in google_pubsub_subscription.pull : s.name]
}

output "notificacoes_inspecao_subscription" {
  description = "Subscription de inspeção no tópico notificacoes (sem worker)."
  value       = google_pubsub_subscription.notificacoes_inspecao.name
}
