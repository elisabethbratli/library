variable "region" {
  type    = string
  default = "fr-par"
}
variable "zone" {
  type    = string
  default = "fr-par-1"
}
variable "name" {
  type    = string
  default = "library"
}
variable "image_tag" {
  description = "Tag of the api and web images pushed to the registry"
  type        = string
  default     = "latest"
}
variable "db_node_type" {
  type    = string
  default = "DB-DEV-S"
}
