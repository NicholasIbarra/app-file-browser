export type NavigationMode = 'push' | 'replace' | 'none'

export interface PathBreadcrumb {
  label: string
  path: string
}

export function getUrlPath(): string | undefined {
  return new URL(window.location.href).searchParams.get('path') ?? undefined
}

export function getUrlSort(): string | undefined {
  return new URL(window.location.href).searchParams.get('sort') ?? undefined
}

export function getSort() : string | undefined { 
  return new URL(window.location.href).searchParams.get('sort') ?? undefined
  
}

export function updateUrl(
  path: string | undefined,
  mode: Exclude<NavigationMode, 'none'>,
  sort?: string,
) {
  const url = new URL(window.location.href)

  if (path) url.searchParams.set('path', path)
  else url.searchParams.delete('path')

  if (sort !== undefined) url.searchParams.set('sort', sort)

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

export function getPathBreadcrumbs(path: string): PathBreadcrumb[] {
  const breadcrumbs: PathBreadcrumb[] = []
  const isAbsoluteUnixPath = path.startsWith('/') && !path.startsWith('//')

  if (isAbsoluteUnixPath) breadcrumbs.push({ label: '/', path: '/' })

  for (const match of path.matchAll(/[^\\/]+/g)) {
    const label = match[0]
    let end = (match.index ?? 0) + label.length

    // Keep the separator on Windows drive roots so "C:" links to "C:\\".
    if (/^[A-Za-z]:$/.test(label) && /[\\/]/.test(path[end] ?? '')) end += 1

    breadcrumbs.push({ label, path: path.slice(0, end) })
  }

  return breadcrumbs
}
