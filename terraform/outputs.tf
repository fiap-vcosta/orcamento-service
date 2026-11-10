output "firestore_database_id" {
  description = "Id do database Firestore nomeado deste serviço."
  value       = google_firestore_database.orcamento.name
}

output "subscription_names" {
  description = "Subscriptions pull consumidas pelo serviço."
  value       = [for s in google_pubsub_subscription.pull : s.name]
}

output "notificacoes_inspecao_subscription" {
  description = "Subscription de inspeção no tópico notificacoes (sem worker)."
  value       = google_pubsub_subscription.notificacoes_inspecao.name
}
