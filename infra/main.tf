resource "scaleway_registry_namespace" "main" {
  name      = var.name
  is_public = false
}

# --- Network: private network so the DB is not exposed to the internet ---
resource "scaleway_vpc_private_network" "main" {
  name = var.name
}

# --- Database (Managed PostgreSQL) ---
resource "random_password" "db" {
  length  = 32
  special = false
}

# Shared password for the web UI (read it with: tofu output -raw app_password)
resource "random_password" "app" {
  length  = 20
  special = false
}

resource "scaleway_rdb_instance" "main" {
  name           = var.name
  engine         = "PostgreSQL-16"
  node_type      = var.db_node_type
  is_ha_cluster  = false
  disable_backup = false
  user_name      = "dbadmin"
  password       = random_password.db.result

  private_network {
    pn_id       = scaleway_vpc_private_network.main.id
    enable_ipam = true
  }
}

resource "scaleway_rdb_database" "library" {
  instance_id = scaleway_rdb_instance.main.id
  name        = "library"
}

# --- Containers ---
resource "scaleway_container_namespace" "main" {
  name = var.name
}

resource "scaleway_container" "api" {
  name               = "${var.name}-api"
  namespace_id       = scaleway_container_namespace.main.id
  registry_image     = "${scaleway_registry_namespace.main.endpoint}/api:${var.image_tag}"
  port               = 8080
  cpu_limit          = 280
  memory_limit_bytes = 268435456
  min_scale          = 0 # scale to zero when idle (cheap; first request is slow)
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
  memory_limit_bytes = 134217728
  min_scale          = 0
  max_scale          = 1
  privacy            = "public"
  protocol           = "http1"

  environment_variables = {
    API_UPSTREAM = trimsuffix(scaleway_container.api.public_endpoint, "/")
    API_HOST     = trimprefix(trimsuffix(scaleway_container.api.public_endpoint, "/"), "https://")
  }
}
