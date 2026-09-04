#!/bin/sh
# Runs as an nginx docker-entrypoint.d/ hook (see Dockerfile) - the base
# image's own entrypoint executes every script here, then starts nginx.
set -eu

# Regenerate config.js from the API_BASE_URL env var at container start,
# so the client doesn't need to be rebuilt to point at a different API
# deployment (Vite env vars are otherwise baked in at build time).
if [ -n "${API_BASE_URL:-}" ]; then
  echo "window.__API_BASE_URL__ = \"$API_BASE_URL\";" > /usr/share/nginx/html/config.js
else
  echo "window.__API_BASE_URL__ = undefined;" > /usr/share/nginx/html/config.js
fi
