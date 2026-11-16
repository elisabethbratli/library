#!/usr/bin/env bash
# Deploys the whole stack. Needs: tofu, docker, and SCW_ACCESS_KEY / SCW_SECRET_KEY / SCW_DEFAULT_PROJECT_ID.
set -euo pipefail
cd "$(dirname "$0")/.."
: "${SCW_SECRET_KEY:?set SCW_SECRET_KEY}" "${SCW_ACCESS_KEY:?set SCW_ACCESS_KEY}" "${SCW_DEFAULT_PROJECT_ID:?set SCW_DEFAULT_PROJECT_ID}"
TAG="${TAG:-$(git rev-parse --short HEAD 2>/dev/null || echo latest)}"

tofu -chdir=infra init
# 1) The registry first: containers can only be created once their images exist.
tofu -chdir=infra apply -auto-approve -target=scaleway_registry_namespace.main
REGISTRY=$(tofu -chdir=infra output -raw registry_endpoint)

# 2) Build and push both images.
echo "$SCW_SECRET_KEY" | docker login "${REGISTRY%%/*}" -u nologin --password-stdin
docker build -t "$REGISTRY/api:$TAG" backend/src/Library.Api
docker build -t "$REGISTRY/web:$TAG" frontend
docker push "$REGISTRY/api:$TAG"
docker push "$REGISTRY/web:$TAG"

# 3) Everything else (asks for confirmation).
tofu -chdir=infra apply -var "image_tag=$TAG"
echo "Web:      $(tofu -chdir=infra output -raw web_url)"
echo "Password: tofu -chdir=infra output -raw app_password"
