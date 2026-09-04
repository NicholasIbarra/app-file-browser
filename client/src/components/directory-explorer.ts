import { getDirectory, reIndexDirectories, type DirectoryContents } from '../api'
import { getParentPath, getPathBreadcrumbs, getUrlPath, updateUrl, type NavigationMode } from '../utils/navigation'
import type { AppShell } from './app-shell'

export class DirectoryExplorer {
  private readonly elements: AppShell

  constructor(elements: AppShell) {
    this.elements = elements
    elements.reIndexButton.addEventListener('click', () => void this.reIndex())
  }

  async load(path?: string, urlMode: NavigationMode = 'none') {
    const { entries, status } = this.elements
    status.hidden = false
    status.textContent = 'Loading…'
    entries.hidden = true
    try {
      const contents = await getDirectory(path)
      if (urlMode !== 'none') updateUrl(path, urlMode)
      this.render(contents, path !== undefined)
      entries.hidden = false
    } catch (error) {
      status.textContent = 'Unable to load this directory.'
      console.error(error)
    }
  }

  private async reIndex() {
    const { reIndexButton, status } = this.elements
    reIndexButton.disabled = true
    reIndexButton.textContent = 'Re-indexing…'
    status.hidden = false
    status.textContent = 'Rebuilding the search index…'
    try {
      await reIndexDirectories()
      await this.load(getUrlPath())
    } catch (error) {
      status.textContent = 'Unable to rebuild the search index.'
      console.error(error)
    } finally {
      reIndexButton.disabled = false
      reIndexButton.textContent = 'Re-index'
    }
  }

  private renderPath(path?: string | null) {
    const target = this.elements.path
    target.replaceChildren()
    if (!path) {
      target.textContent = 'Root'
      return
    }
    const breadcrumbs = getPathBreadcrumbs(path)
    breadcrumbs.forEach((breadcrumb, index) => {
      if (index > 0 && breadcrumbs[index - 1].label !== '/') target.append(document.createTextNode(' / '))
      const link = document.createElement('a')
      const url = new URL(window.location.href)
      url.searchParams.set('path', breadcrumb.path)
      link.href = url.toString()
      link.textContent = breadcrumb.label
      link.addEventListener('click', (event) => {
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
        event.preventDefault()
        void this.load(breadcrumb.path, 'push')
      })
      target.append(link)
    })
  }

  private render(contents: DirectoryContents, canGoUp: boolean) {
    const { entries, status } = this.elements
    this.renderPath(contents.path)
    status.hidden = true
    entries.replaceChildren()
    if (canGoUp && contents.path) {
      const item = document.createElement('li')
      const button = document.createElement('button')
      button.type = 'button'
      button.textContent = '..'
      button.addEventListener('click', () => void this.load(getParentPath(contents.path!), 'push'))
      item.append(button)
      entries.append(item)
    }
    for (const entry of contents.entries ?? []) {
      const item = document.createElement('li')
      if (entry.type === 'Directory' && entry.path) {
        const button = document.createElement('button')
        button.type = 'button'
        button.textContent = `${entry.name ?? entry.path}/`
        button.addEventListener('click', () => void this.load(entry.path!, 'push'))
        item.append(button)
      } else {
        const name = document.createElement('span')
        name.textContent = entry.name ?? entry.path ?? '(unnamed file)'
        item.append(name)
      }
      entries.append(item)
    }
    if ((contents.entries?.length ?? 0) === 0) {
      const item = document.createElement('li')
      item.className = 'empty'
      item.textContent = 'This folder is empty.'
      entries.append(item)
    }
  }
}
