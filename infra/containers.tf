# Shared password for the web UI (read it with: tofu output -raw app_password)
resource "random_password" "app" {
  length  = 20
  special = false
}

resource "scaleway_container_namespace" "main" {
  name = var.name
}

locals {
  api_url = trimsuffix(scaleway_container.api.public_endpoint, "/")
}

resource "scaleway_container" "api" {
  name               = "${var.name}-api"
  namespace_id       = scaleway_container_namespace.main.id
  registry_image     = "${scaleway_registry_namespace.main.endpoint}/api:${var.image_tag}"
  port               = 8080
  cpu_limit          = 280
  memory_limit_bytes = 268435456 # 256 MiB
  min_scale          = 0         # scale to zero when idle (cheap; first request is slow)
  max_scale          = 1
  privacy            = "public"
  protocol           = "http1"
  private_network_id = scaleway_vpc_private_network.main.id

  secret_environment_variables = {
    "Auth__Password" = random_password.app.result
    "ConnectionStrings__Library" = join(";", [
      "Host=${scaleway_rdb_instance.main.private_network[0].ip}",
      "Port=${scaleway_rdb_instance.main.private_network[0].port}",
      "Database=${scaleway_rdb_database.library.name}",
      "Username=${scaleway_rdb_instance.main.user_name}",
      "Password=${random_password.db.result}",
      "SSL Mode=Require",
      "Trust Server Certificate=true",
    ])
  }
}

resource "scaleway_container" "web" {
  name               = "${var.name}-web"
  namespace_id       = scaleway_container_namespace.main.id
  registry_image     = "${scaleway_registry_namespace.main.endpoint}/web:${var.image_tag}"
  port               = 8080
  cpu_limit          = 140
  memory_limit_bytes = 134217728 # 128 MiB
  min_scale          = 0
  max_scale          = 1
  privacy            = "public"
  protocol           = "http1"

  # nginx in the web container proxies /api to the API container.
  environment_variables = {
    API_UPSTREAM = local.api_url
    API_HOST     = trimprefix(local.api_url, "https://")
  }
}
