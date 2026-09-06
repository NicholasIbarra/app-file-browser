import { getSettings, searchFiles, searchFilesSemantic, type SearchResult } from '../api'
import { getParentPath } from '../utils/navigation'
import { formatFileSize } from '../utils/file-size'
import type { AppShell } from './app-shell'

export class SearchDialog {
  private request?: AbortController
  private aiRequest?: AbortController
  private semanticSearchEnabled = false
  private readonly elements: AppShell
  private readonly navigate: (path?: string) => void

  constructor(elements: AppShell, navigate: (path?: string) => void) {
    this.elements = elements
    this.navigate = navigate

    this.setupAiSearch()
    void this.loadSettings()
    
    elements.searchButton.addEventListener('click', () => this.open())
    elements.searchInput.addEventListener('input', () => void this.search(elements.searchInput.value))
    elements.searchOverlay.addEventListener('click', (event) => {
      if (event.target === elements.searchOverlay) {
        this.close()
      }
    })

    document.addEventListener('keydown', (event) => this.handleKeydown(event))
  }

  private async loadSettings() {
    try {
      const settings = await getSettings()

      this.semanticSearchEnabled = settings.semanticSearchEnabled === true
      this.elements.searchOverlay.querySelector<HTMLButtonElement>('#ai-search-tab')!.hidden = !this.semanticSearchEnabled
    
    } catch (error) {
      console.error('Unable to load search settings.', error)
    }
  }

  private open() {
    this.elements.searchOverlay.hidden = false
    document.body.classList.add('search-open')
    this.elements.searchOverlay.querySelector<HTMLInputElement>('[role="tabpanel"]:not([hidden]) input')?.focus()
  }

  private setupAiSearch() {
    const overlay = this.elements.searchOverlay
    const tabs = Array.from(overlay.querySelectorAll<HTMLButtonElement>('[role="tab"]'))
    const aiInput = overlay.querySelector<HTMLInputElement>('#ai-search-input')!

    const selectTab = (selected: HTMLButtonElement) => {
      if (selected.hidden) {
        return
      }

      this.request?.abort()
      this.request = undefined
      
      if (this.aiRequest) {
        this.resetAiSearch()
      }

      for (const tab of tabs) {
        const active = tab === selected

        tab.setAttribute('aria-selected', String(active))
        tab.tabIndex = active ? 0 : -1
        
        overlay.querySelector<HTMLElement>(`#${tab.getAttribute('aria-controls')}`)!.hidden = !active
      }
      
      if (selected.id === 'file-search-tab') {
        void this.search(this.elements.searchInput.value)
      }
    }

    tabs.forEach((tab) => {
      tab.addEventListener('click', () => selectTab(tab))
      tab.addEventListener('keydown', (event) => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
          return
        }

        event.preventDefault()
        
        const visibleTabs = tabs.filter((candidate) => !candidate.hidden)
        const index = visibleTabs.indexOf(tab)
        const direction = event.key === 'ArrowLeft' ? -1 : 1
        const next = event.key === 'Home' ? visibleTabs[0] : event.key === 'End' ? visibleTabs[visibleTabs.length - 1] : visibleTabs[(index + direction + visibleTabs.length) % visibleTabs.length]
        
        selectTab(next!)
        next!.focus()
      })
    })

    aiInput.addEventListener('input', () => this.resetAiSearch())
    overlay.querySelector('#ai-search-form')!.addEventListener('submit', (event) => {
      event.preventDefault()
      const query = aiInput.value.trim()
      
      if (!query) {
        return
      }

      void this.searchAi(query)
    })

    overlay.querySelectorAll<HTMLButtonElement>('.ai-search-examples button').forEach((button) => {
      button.addEventListener('click', () => {
        aiInput.value = button.textContent ?? ''
        this.resetAiSearch()
        aiInput.focus()
      })
    })
  }

  private resetAiSearch() {
    this.aiRequest?.abort()
    this.aiRequest = undefined

    const overlay = this.elements.searchOverlay
    overlay.querySelector<HTMLElement>('#ai-search-preview')!.hidden = true
    
    const results = overlay.querySelector<HTMLUListElement>('#ai-search-results')!

    results.replaceChildren()
    results.hidden = true
    overlay.querySelector<HTMLButtonElement>('.ai-search-submit')!.disabled = false
    results.setAttribute('aria-busy', 'false')
  }

  private async searchAi(query: string) {
    if (!this.semanticSearchEnabled) {
      return
    }

    this.resetAiSearch()
    
    const overlay = this.elements.searchOverlay
    const status = overlay.querySelector<HTMLDivElement>('#ai-search-preview')!
    const results = overlay.querySelector<HTMLUListElement>('#ai-search-results')!
    const submit = overlay.querySelector<HTMLButtonElement>('.ai-search-submit')!
    const request = new AbortController()
    
    this.aiRequest = request
    status.textContent = 'Searching…'
    status.hidden = false
    submit.disabled = true
    results.setAttribute('aria-busy', 'true')

    try {
      const response = await searchFilesSemantic(query, undefined, request.signal)
      if (this.aiRequest !== request) {
        return
      }

      const matches = response.results ?? []
      
      this.renderResults(matches, results, status)
      status.textContent = response.message?.trim()
        || (matches.length ? `Found ${matches.length} matching files.` : 'No matching files or folders.')

      status.hidden = false
    } catch (error) {
      if (request.signal.aborted || this.aiRequest !== request) {
        return
      }

      status.textContent = 'Unable to search right now. Please try again.'
      console.error(error)
    } finally {
      if (this.aiRequest === request) {
        this.aiRequest = undefined
        submit.disabled = false
        results.setAttribute('aria-busy', 'false')
      }
    }
  }

  private close() {
    const { searchButton, searchInput, searchOverlay, searchResults, searchStatus } = this.elements

    this.request?.abort()
    this.request = undefined
    this.resetAiSearch()
    
    searchOverlay.hidden = true
    document.body.classList.remove('search-open')
    
    searchInput.value = ''
    
    this.elements.searchOverlay.querySelector<HTMLFormElement>('#ai-search-form')!.reset()
    this.elements.searchOverlay.querySelector<HTMLElement>('#ai-search-preview')!.hidden = true
    
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
      if (this.elements.searchOverlay.hidden) {
        this.open()
      }

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

      if (this.request === request) {
        this.renderResults(results)
      }

    } catch (error) {
      if (request.signal.aborted) {
        return
      }

      searchResults.hidden = true
      searchStatus.textContent = 'Unable to search right now.'
      
      console.error(error)
    } finally {
      if (this.request === request) {
        this.request = undefined
      }
    }
  }

  private renderResults(
    results: SearchResult[],
    searchResults = this.elements.searchResults,
    searchStatus: HTMLElement = this.elements.searchStatus,
  ) {

    searchResults.replaceChildren()

    for (const result of results) {
      const item = document.createElement('li')
      const button = document.createElement('button')

      button.type = 'button'
      button.className = 'search-result'

      button.addEventListener('click', () => {
        const resultPath = result.path
        
        if (!resultPath) {
          return
        }

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
      const metadata: string[] = []
      if (!result.isDirectory && result.size != null) {
        metadata.push(formatFileSize(result.size))
      }
      if (result.lastModified) {
        const modified = new Date(result.lastModified)
        if (!Number.isNaN(modified.getTime())) {
          metadata.push(`Modified ${modified.toLocaleString()}`)
        }
      }
      if (metadata.length) {
        const values = document.createElement('span')
        values.className = 'search-result-metadata'
        values.textContent = metadata.join(' · ')
        details.append(values)
      }
      button.append(icon, details)
      item.append(button)
      searchResults.append(item)
    }
    
    searchStatus.hidden = results.length > 0
    searchStatus.textContent = 'No matching files or folders.'
    searchResults.hidden = results.length === 0
  }
}
