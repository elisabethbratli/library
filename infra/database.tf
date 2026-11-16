# The database is only reachable from the private network, not from the internet.
resource "scaleway_vpc_private_network" "main" {
  name = var.name
}

resource "random_password" "db" {
  length  = 32
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
