output "registry_endpoint" {
  value = scaleway_registry_namespace.main.endpoint
}
output "web_url" {
  value = "https://${scaleway_container.web.domain_name}"
}
output "api_url" {
  value = "https://${scaleway_container.api.domain_name}"
}
