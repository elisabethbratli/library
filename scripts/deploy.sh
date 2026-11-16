#!/usr/bin/env bash
# Deploys the whole stack. Requires: tofu, docker, and SCW_ACCESS_KEY / SCW_SECRET_KEY / SCW_DEFAULT_PROJECT_ID.
set -euo pipefail
cd "$(dirname "$0")/.."
TAG="${TAG:-$(git rev-parse --short HEAD 2>/dev/null || echo latest)}"

tofu -chdir=infra init
# 1) registry first (containers need images to exist before they can be created)
tofu -chdir=infra apply -auto-approve -target=scaleway_registry_namespace.main
REGISTRY=$(tofu -chdir=infra output -raw registry_endpoint)

# 2) build + push
echo "$SCW_SECRET_KEY" | docker login "${REGISTRY%%/*}" -u nologin --password-stdin
docker build -t "$REGISTRY/api:$TAG" backend/Library.Api
docker build -t "$REGISTRY/web:$TAG" frontend
docker push "$REGISTRY/api:$TAG"
docker push "$REGISTRY/web:$TAG"

# 3) everything else
tofu -chdir=infra apply -var "image_tag=$TAG"
