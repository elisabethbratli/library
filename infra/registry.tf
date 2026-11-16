resource "scaleway_registry_namespace" "main" {
  name      = var.name
  is_public = false
}
