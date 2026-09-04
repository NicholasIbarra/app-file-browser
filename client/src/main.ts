import './style.css'
import { getDirectory, type DirectoryContents } from './api'
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
      <button id="theme-toggle" type="button"></button>
    </header>
    <p id="current-path" aria-live="polite"></p>
    <p id="status" role="status">Loading…</p>
    <ul id="entries" aria-label="Directory contents"></ul>
  </main>
`

const pathElement = document.querySelector<HTMLParagraphElement>('#current-path')!
const statusElement = document.querySelector<HTMLParagraphElement>('#status')!
const entriesElement = document.querySelector<HTMLUListElement>('#entries')!
const themeToggle = document.querySelector<HTMLButtonElement>('#theme-toggle')!

initializeThemeToggle(themeToggle)

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
