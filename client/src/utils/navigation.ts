export type NavigationMode = 'push' | 'replace' | 'none'

export function getUrlPath(): string | undefined {
  return new URL(window.location.href).searchParams.get('path') ?? undefined
}

export function updateUrl(
  path: string | undefined,
  mode: Exclude<NavigationMode, 'none'>,
) {
  const url = new URL(window.location.href)

  if (path) url.searchParams.set('path', path)
  else url.searchParams.delete('path')

  window.history[mode === 'push' ? 'pushState' : 'replaceState']({}, '', url)
}

export function getParentPath(path: string): string | undefined {
  const trimmed = path.replace(/[\\/]+$/, '')

  if (!trimmed || /^[A-Za-z]:$/.test(trimmed)) return undefined

  const isUncPath = /^[\\/]{2}/.test(trimmed)
  if (isUncPath && trimmed.split(/[\\/]+/).filter(Boolean).length <= 2) {
    return undefined
  }

  const separatorIndex = Math.max(
    trimmed.lastIndexOf('/'),
    trimmed.lastIndexOf('\\'),
  )

  if (separatorIndex < 0) return undefined
  if (separatorIndex === 0) return trimmed[0]
  if (separatorIndex === 2 && trimmed[1] === ':') {
    return trimmed.slice(0, 3)
  }

  return trimmed.slice(0, separatorIndex)
}
