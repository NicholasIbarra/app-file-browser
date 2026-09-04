import './style.css'
import { getDirectory, type DirectoryContents } from './api'

const app = document.querySelector<HTMLDivElement>('#app')!
const history: Array<string | undefined> = []
const savedTheme = localStorage.getItem('theme')
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
const initialTheme = savedTheme ?? (prefersDark ? 'dark' : 'light')

document.documentElement.dataset.theme = initialTheme

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

function updateThemeToggle() {
  const isDark = document.documentElement.dataset.theme === 'dark'
  themeToggle.textContent = isDark ? 'Light mode' : 'Dark mode'
  themeToggle.setAttribute('aria-pressed', String(isDark))
}

themeToggle.addEventListener('click', () => {
  const nextTheme =
    document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'
  document.documentElement.dataset.theme = nextTheme
  localStorage.setItem('theme', nextTheme)
  updateThemeToggle()
})

updateThemeToggle()

function render(contents: DirectoryContents) {
  pathElement.textContent = contents.path || 'Root'
  statusElement.hidden = true
  entriesElement.replaceChildren()

  if (history.length > 0) {
    const parentItem = document.createElement('li')
    const parentButton = document.createElement('button')
    parentButton.type = 'button'
    parentButton.textContent = '..'
    parentButton.addEventListener('click', () => {
      const parentPath = history.pop()
      void loadDirectory(parentPath, false)
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
        void loadDirectory(entry.path!, true)
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

async function loadDirectory(path?: string, rememberCurrent = false) {
  statusElement.hidden = false
  statusElement.textContent = 'Loading…'
  entriesElement.hidden = true

  try {
    const contents = await getDirectory(path)
    if (rememberCurrent) history.push(pathElement.dataset.path || undefined)
    pathElement.dataset.path = contents.path ?? ''
    render(contents)
    entriesElement.hidden = false
  } catch (error) {
    statusElement.textContent = 'Unable to load this directory.'
    console.error(error)
  }
}

void loadDirectory()
