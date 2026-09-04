import {
  copyEntry,
  deleteEntry,
  getDirectory,
  moveEntry,
  reIndexDirectories,
  uploadFile,
  type DirectoryContents,
} from '../api'
import { getParentPath, getPathBreadcrumbs, getUrlPath, updateUrl, type NavigationMode } from '../utils/navigation'
import type { AppShell } from './app-shell'

export class DirectoryExplorer {
  private readonly elements: AppShell
  private currentPath = '/'

  constructor(elements: AppShell) {
    this.elements = elements
    elements.reIndexButton.addEventListener('click', () => void this.reIndex())
    elements.uploadButton.addEventListener('click', () => elements.uploadInput.click())
    elements.uploadInput.addEventListener('change', () => void this.uploadSelectedFiles())
  }

  async load(path?: string, urlMode: NavigationMode = 'none') {
    const { entries, status } = this.elements
    status.hidden = false
    status.textContent = 'Loading…'
    entries.hidden = true
    try {
      const contents = await getDirectory(path)
      this.currentPath = contents.path || '/'
      if (urlMode !== 'none') updateUrl(path, urlMode)
      this.render(contents, path !== undefined)
      entries.hidden = false
    } catch (error) {
      status.textContent = 'Unable to load this directory.'
      console.error(error)
    }
  }

  private async uploadSelectedFiles() {
    const { uploadButton, uploadInput } = this.elements
    const files = [...(uploadInput.files ?? [])]
    if (files.length === 0) return

    uploadButton.disabled = true
    this.showStatus(`Uploading ${files.length === 1 ? files[0].name : `${files.length} files`}…`)
    try {
      for (const file of files) {
        await uploadFile(this.joinPath(this.currentPath, file.name), file)
      }
      await this.load(this.currentPath)
    } catch (error) {
      this.showStatus('Unable to upload the selected file. It may already exist.')
      console.error(error)
    } finally {
      uploadInput.value = ''
      uploadButton.disabled = false
    }
  }

  private async move(path: string) {
    const destinationPath = window.prompt('Move to path:', path)
    if (!destinationPath || destinationPath === path) return

    await this.runOperation(
      'Moving…',
      'Unable to move this item.',
      () => moveEntry({ sourcePath: path, destinationPath, overwrite: false }),
    )
  }

  private async copy(path: string) {
    const destinationPath = window.prompt('Copy to path:', `${path}-copy`)
    if (!destinationPath || destinationPath === path) return

    await this.runOperation(
      'Copying…',
      'Unable to copy this item.',
      () => copyEntry({ sourcePath: path, destinationPath, overwrite: false }),
    )
  }

  private async delete(path: string, isDirectory: boolean) {
    const message = isDirectory
      ? `Delete “${path}” and everything inside it?`
      : `Delete “${path}”?`
    if (!window.confirm(message)) return

    await this.runOperation(
      'Deleting…',
      'Unable to delete this item.',
      () => deleteEntry(path, isDirectory),
    )
  }

  private async runOperation(
    pendingMessage: string,
    errorMessage: string,
    operation: () => Promise<void>,
  ) {
    this.setActionsDisabled(true)
    this.showStatus(pendingMessage)
    try {
      await operation()
      await this.load(this.currentPath)
    } catch (error) {
      this.showStatus(errorMessage)
      console.error(error)
    } finally {
      this.setActionsDisabled(false)
    }
  }

  private showStatus(message: string) {
    this.elements.status.hidden = false
    this.elements.status.textContent = message
  }

  private setActionsDisabled(disabled: boolean) {
    for (const button of this.elements.entries.querySelectorAll<HTMLButtonElement>('.entry-action')) {
      button.disabled = disabled
    }
  }

  private joinPath(directory: string, name: string) {
    return directory === '/' ? `/${name}` : `${directory.replace(/\/$/, '')}/${name}`
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
      item.className = 'entry-row'
      const entryMain = document.createElement('span')
      entryMain.className = 'entry-main'
      if (entry.type === 'Directory' && entry.path) {
        const button = document.createElement('button')
        button.type = 'button'
        button.textContent = `${entry.name ?? entry.path}/`
        button.addEventListener('click', () => void this.load(entry.path!, 'push'))
        entryMain.append(button)
      } else {
        const name = document.createElement('span')
        name.textContent = entry.name ?? entry.path ?? '(unnamed file)'
        entryMain.append(name)
      }
      item.append(entryMain)

      if (entry.path) {
        const actions = document.createElement('span')
        actions.className = 'entry-actions'
        actions.append(
          this.createAction('Move', () => this.move(entry.path!)),
          this.createAction('Copy', () => this.copy(entry.path!)),
          this.createAction('Delete', () => this.delete(entry.path!, entry.type === 'Directory'), 'danger'),
        )
        item.append(actions)
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

  private createAction(
    label: string,
    action: () => void,
    variant?: 'danger',
  ) {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = `entry-action${variant ? ` ${variant}` : ''}`
    button.textContent = label
    button.addEventListener('click', action)
    return button
  }
}
