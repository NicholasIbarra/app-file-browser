import './style.css'
import { connectFileHub } from './api/file-hub'
import { renderAppShell } from './components/app-shell'
import { DirectoryExplorer } from './components/directory-explorer'
import { SearchDialog } from './components/search-dialog'
import { getUrlPath } from './utils/navigation'
import { initializeThemeToggle } from './utils/theme'

const root = document.querySelector<HTMLDivElement>('#app')

if (!root) {
    throw new Error('App root is missing')
}

const appShell = renderAppShell(root)
const explorer = new DirectoryExplorer(appShell)

initializeThemeToggle(appShell.themeToggle)

new SearchDialog(
    appShell, 
    (path) => void explorer.load(path, 'push'));

// Handle the back button
window.addEventListener(
    'popstate', 
    () => void explorer.load(getUrlPath()))

void explorer.load(getUrlPath())

const disconnectFileHub = connectFileHub(() => explorer.refresh())
import.meta.hot?.dispose(disconnectFileHub)
