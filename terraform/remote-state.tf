data "terraform_remote_state" "infra_bootstrap" {
  backend = "gcs"

  config = {
    bucket = "vcosta-fiap-tech-challenge-tfstate"
    prefix = "infra-bootstrap"
  }
}
