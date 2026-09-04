import { searchFiles, type SearchResult } from '../api'
import { getParentPath } from '../utils/navigation'
import type { AppShell } from './app-shell'

export class SearchDialog {
  private request?: AbortController
  private readonly elements: AppShell
  private readonly navigate: (path?: string) => void

  constructor(elements: AppShell, navigate: (path?: string) => void) {
    this.elements = elements
    this.navigate = navigate
    elements.searchButton.addEventListener('click', () => this.open())
    elements.searchInput.addEventListener('input', () => void this.search(elements.searchInput.value))
    elements.searchOverlay.addEventListener('click', (event) => {
      if (event.target === elements.searchOverlay) this.close()
    })
    document.addEventListener('keydown', (event) => this.handleKeydown(event))
  }

  private open() {
    this.elements.searchOverlay.hidden = false
    document.body.classList.add('search-open')
    this.elements.searchInput.focus()
  }

  private close() {
    const { searchButton, searchInput, searchOverlay, searchResults, searchStatus } = this.elements
    this.request?.abort()
    this.request = undefined
    searchOverlay.hidden = true
    document.body.classList.remove('search-open')
    searchInput.value = ''
    searchResults.replaceChildren()
    searchResults.hidden = true
    searchStatus.hidden = false
    searchStatus.textContent = 'Start typing to search your files.'
    searchButton.focus()
  }

  private handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && !this.elements.searchOverlay.hidden) {
      this.close()
      return
    }
    if (event.key.toLowerCase() === 'k' && (event.metaKey || event.ctrlKey)) {
      event.preventDefault()
      if (this.elements.searchOverlay.hidden) this.open()
      else this.close()
    }
  }

  private async search(query: string) {
    this.request?.abort()
    const normalized = query.trim()
    const { searchResults, searchStatus } = this.elements
    if (!normalized) {
      this.request = undefined
      searchResults.replaceChildren()
      searchResults.hidden = true
      searchStatus.hidden = false
      searchStatus.textContent = 'Start typing to search your files.'
      return
    }
    const request = new AbortController()
    this.request = request
    searchResults.hidden = true
    searchStatus.hidden = false
    searchStatus.textContent = 'Searching…'
    try {
      const results = await searchFiles(normalized, undefined, request.signal)
      if (this.request === request) this.renderResults(results)
    } catch (error) {
      if (request.signal.aborted) return
      searchResults.hidden = true
      searchStatus.textContent = 'Unable to search right now.'
      console.error(error)
    } finally {
      if (this.request === request) this.request = undefined
    }
  }

  private renderResults(results: SearchResult[]) {
    const { searchResults, searchStatus } = this.elements
    searchResults.replaceChildren()
    for (const result of results) {
      const item = document.createElement('li')
      const button = document.createElement('button')
      button.type = 'button'
      button.className = 'search-result'
      button.addEventListener('click', () => {
        const resultPath = result.path
        if (!resultPath) return
        this.close()
        this.navigate(result.isDirectory ? resultPath : getParentPath(resultPath))
      })
      const icon = document.createElement('span')
      icon.className = 'search-result-icon'
      icon.setAttribute('aria-hidden', 'true')
      icon.textContent = result.isDirectory ? '📁' : '📄'
      const details = document.createElement('span')
      details.className = 'search-result-details'
      const name = document.createElement('strong')
      name.textContent = result.name ?? result.path ?? '(unnamed file)'
      const path = document.createElement('span')
      path.textContent = result.path ?? ''
      details.append(name, path)
      button.append(icon, details)
      item.append(button)
      searchResults.append(item)
    }
    searchStatus.hidden = results.length > 0
    searchStatus.textContent = 'No matching files or folders.'
    searchResults.hidden = results.length === 0
  }
}
