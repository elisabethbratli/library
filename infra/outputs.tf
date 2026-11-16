output "registry_endpoint" {
  value = scaleway_registry_namespace.main.endpoint
}
output "web_url" {
  value = scaleway_container.web.public_endpoint
}
output "api_url" {
  value = scaleway_container.api.public_endpoint
}
output "app_password" {
  value     = random_password.app.result
  sensitive = true
}
