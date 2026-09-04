/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

interface Window {
  /**
   * Injected at container start (see client/docker-entrypoint.sh) so the
   * API URL can be set per-deployment without rebuilding the client image.
   */
  __API_BASE_URL__?: string
}
