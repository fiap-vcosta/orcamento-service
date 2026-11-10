terraform {
  backend "gcs" {
    bucket = "vcosta-fiap-tech-challenge-tfstate"
    prefix = "orcamento-service"
  }
}
