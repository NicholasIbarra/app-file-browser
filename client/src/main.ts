import './style.css'
import {
  getDirectory,
  searchFiles,
  type DirectoryContents,
  type SearchResult,
} from './api'
import {
  getParentPath,
  getPathBreadcrumbs,
  getUrlPath,
  updateUrl,
  type NavigationMode,
} from './utils/navigation'
import { initializeThemeToggle } from './utils/theme'

const app = document.querySelector<HTMLDivElement>('#app')!

app.innerHTML = `
  <main class="explorer">
    <header>
      <h1>File Explorer</h1>
      <div class="header-actions">
        <button id="search-button" type="button" aria-haspopup="dialog">
          <span>Search</span>
          <kbd aria-hidden="true">⌘ K</kbd>
        </button>
        <button id="theme-toggle" type="button"></button>
      </div>
    </header>
    <p id="current-path" aria-live="polite"></p>
    <p id="status" role="status">Loading…</p>
    <ul id="entries" aria-label="Directory contents"></ul>
  </main>
  <div id="search-overlay" class="search-overlay" hidden>
    <section
      class="search-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="search-title"
    >
      <h2 id="search-title" class="visually-hidden">Search files</h2>
      <div class="search-field">
        <svg aria-hidden="true" viewBox="0 0 24 24">
          <path d="m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z" />
        </svg>
        <input
          id="search-input"
          type="search"
          placeholder="Search files and folders"
          autocomplete="off"
          spellcheck="false"
        />
        <kbd>Esc</kbd>
      </div>
      <p id="search-status" class="search-hint" role="status">
        Start typing to search your files.
      </p>
      <ul id="search-results" aria-label="Search results" hidden></ul>
    </section>
  </div>
`

const pathElement = document.querySelector<HTMLParagraphElement>('#current-path')!
const statusElement = document.querySelector<HTMLParagraphElement>('#status')!
const entriesElement = document.querySelector<HTMLUListElement>('#entries')!
const themeToggle = document.querySelector<HTMLButtonElement>('#theme-toggle')!
const searchButton = document.querySelector<HTMLButtonElement>('#search-button')!
const searchOverlay = document.querySelector<HTMLDivElement>('#search-overlay')!
const searchInput = document.querySelector<HTMLInputElement>('#search-input')!
const searchStatus = document.querySelector<HTMLParagraphElement>('#search-status')!
const searchResults = document.querySelector<HTMLUListElement>('#search-results')!

let searchRequest: AbortController | undefined

initializeThemeToggle(themeToggle)

function openSearch() {
  searchOverlay.hidden = false
  document.body.classList.add('search-open')
  searchInput.focus()
}

function closeSearch() {
  searchRequest?.abort()
  searchRequest = undefined
  searchOverlay.hidden = true
  document.body.classList.remove('search-open')
  searchInput.value = ''
  searchResults.replaceChildren()
  searchResults.hidden = true
  searchStatus.hidden = false
  searchStatus.textContent = 'Start typing to search your files.'
  searchButton.focus()
}

function renderSearchResults(results: SearchResult[]) {
  searchResults.replaceChildren()

  for (const result of results) {
    const item = document.createElement('li')
    const resultContent = document.createElement('button')
    resultContent.type = 'button'
    resultContent.className = 'search-result'
    resultContent.addEventListener('click', () => {
      if (!result.path) return

      const destination = result.isDirectory
        ? result.path
        : getParentPath(result.path)

      closeSearch()
      void loadDirectory(destination, 'push')
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
    resultContent.append(icon, details)
    item.append(resultContent)
    searchResults.append(item)
  }

  searchStatus.hidden = results.length > 0
  searchStatus.textContent = 'No matching files or folders.'
  searchResults.hidden = results.length === 0
}

async function runSearch(query: string) {
  searchRequest?.abort()

  const normalizedQuery = query.trim()
  if (!normalizedQuery) {
    searchRequest = undefined
    searchResults.replaceChildren()
    searchResults.hidden = true
    searchStatus.hidden = false
    searchStatus.textContent = 'Start typing to search your files.'
    return
  }

  const request = new AbortController()
  searchRequest = request
  searchResults.hidden = true
  searchStatus.hidden = false
  searchStatus.textContent = 'Searching…'

  try {
    const results = await searchFiles(normalizedQuery, undefined, request.signal)
    if (searchRequest !== request) return
    renderSearchResults(results)
  } catch (error) {
    if (request.signal.aborted) return
    searchResults.hidden = true
    searchStatus.textContent = 'Unable to search right now.'
    console.error(error)
  } finally {
    if (searchRequest === request) searchRequest = undefined
  }
}

searchButton.addEventListener('click', openSearch)
searchInput.addEventListener('input', () => {
  void runSearch(searchInput.value)
})

searchOverlay.addEventListener('click', (event) => {
  if (event.target === searchOverlay) closeSearch()
})

document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape' && !searchOverlay.hidden) {
    closeSearch()
    return
  }

  if (event.key.toLowerCase() === 'k' && (event.metaKey || event.ctrlKey)) {
    event.preventDefault()
    if (searchOverlay.hidden) openSearch()
    else closeSearch()
  }
})

function renderPath(path?: string | null) {
  pathElement.replaceChildren()

  if (!path) {
    pathElement.textContent = 'Root'
    return
  }

  const breadcrumbs = getPathBreadcrumbs(path)

  breadcrumbs.forEach((breadcrumb, index) => {
    if (index > 0 && breadcrumbs[index - 1].label !== '/') {
      pathElement.append(document.createTextNode(' / '))
    }

    const link = document.createElement('a')
    const url = new URL(window.location.href)
    url.searchParams.set('path', breadcrumb.path)
    link.href = url.toString()
    link.textContent = breadcrumb.label
    link.addEventListener('click', (event) => {
      if (
        event.button !== 0 ||
        event.metaKey ||
        event.ctrlKey ||
        event.shiftKey ||
        event.altKey
      ) {
        return
      }

      event.preventDefault()
      void loadDirectory(breadcrumb.path, 'push')
    })
    pathElement.append(link)
  })
}

function render(contents: DirectoryContents, canGoUp: boolean) {
  renderPath(contents.path)
  statusElement.hidden = true
  entriesElement.replaceChildren()

  if (canGoUp && contents.path) {
    const parentItem = document.createElement('li')
    const parentButton = document.createElement('button')
    parentButton.type = 'button'
    parentButton.textContent = '..'
    parentButton.addEventListener('click', () => {
      void loadDirectory(getParentPath(contents.path!), 'push')
    })
    parentItem.append(parentButton)
    entriesElement.append(parentItem)
  }

  for (const entry of contents.entries ?? []) {
    const item = document.createElement('li')

    if (entry.type === 'Directory' && entry.path) {
      const button = document.createElement('button')
      button.type = 'button'
      button.textContent = `${entry.name ?? entry.path}/`
      button.addEventListener('click', () => {
        void loadDirectory(entry.path!, 'push')
      })
      item.append(button)
    } else {
      const fileName = document.createElement('span')
      fileName.textContent = entry.name ?? entry.path ?? '(unnamed file)'
      item.append(fileName)
    }

    entriesElement.append(item)
  }

  if ((contents.entries?.length ?? 0) === 0) {
    const emptyItem = document.createElement('li')
    emptyItem.className = 'empty'
    emptyItem.textContent = 'This folder is empty.'
    entriesElement.append(emptyItem)
  }
}

async function loadDirectory(
  path?: string,
  urlMode: NavigationMode = 'none',
) {
  statusElement.hidden = false
  statusElement.textContent = 'Loading…'
  entriesElement.hidden = true

  try {
    const contents = await getDirectory(path)
    if (urlMode !== 'none') updateUrl(path, urlMode)
    render(contents, path !== undefined)
    entriesElement.hidden = false
  } catch (error) {
    statusElement.textContent = 'Unable to load this directory.'
    console.error(error)
  }
}

// Listen for clicking the back button
window.addEventListener('popstate', () => {
  void loadDirectory(getUrlPath())
})

void loadDirectory(getUrlPath())
