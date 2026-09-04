import './style.css'
import { renderAppShell } from './components/app-shell'
import { DirectoryExplorer } from './components/directory-explorer'
import { SearchDialog } from './components/search-dialog'
import { getUrlPath } from './utils/navigation'
import { initializeThemeToggle } from './utils/theme'

const root = document.querySelector<HTMLDivElement>('#app')
if (!root) throw new Error('App root is missing')

const elements = renderAppShell(root)
const explorer = new DirectoryExplorer(elements)

initializeThemeToggle(elements.themeToggle)
new SearchDialog(elements, (path) => void explorer.load(path, 'push'))

window.addEventListener('popstate', () => void explorer.load(getUrlPath()))
void explorer.load(getUrlPath())
