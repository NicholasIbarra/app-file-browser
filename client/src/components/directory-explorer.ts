import {
  copyEntry,
  deleteEntry,
  downloadFile,
  getDirectory,
  moveEntry,
  uploadFile,
  type DirectoryContents,
} from '../api'
import { formatFileSize } from '../utils/file-size'
import { getParentPath, getPathBreadcrumbs, updateUrl, type NavigationMode } from '../utils/navigation'
import type { AppShell } from './app-shell'

export class DirectoryExplorer {
  private readonly elements: AppShell
  private currentPath = '/'

  constructor(elements: AppShell) {
    this.elements = elements
    elements.uploadButton.addEventListener('click', () => elements.uploadInput.click())
    elements.uploadInput.addEventListener('change', () => void this.uploadSelectedFiles())

    document.addEventListener('click', (event) => {
      if (!(event.target instanceof Element) || event.target.closest('.entry-actions')) return
      this.closeActionMenus()
    })
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') this.closeActionMenus(true)
    })
  }

  async load(path?: string, urlMode: NavigationMode = 'none') {
    const { entries, status } = this.elements
    
    // Set loading state
    status.hidden = false
    status.textContent = 'Loading…'
    entries.hidden = true
   
    try {
      const contents = await getDirectory(path)
      this.currentPath = contents.path || '/'
      
      if (urlMode !== 'none') {
        // Push to browser stack for back button support
        updateUrl(path, urlMode)
      }
      
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
    
    if (files.length === 0) {
      return
    }

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
    if (!destinationPath || destinationPath === path) {
      return
    }

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

  private async download(path: string, fileName: string) {
    this.setActionsDisabled(true)
    this.showStatus(`Downloading ${fileName}…`)

    try {
      const content = await downloadFile(path)
      const url = URL.createObjectURL(content)
      const link = document.createElement('a')

      link.href = url
      link.download = fileName
      link.hidden = true
      
      document.body.append(link)
      
      link.click()
      link.remove()
      window.setTimeout(() => URL.revokeObjectURL(url), 0)
      this.elements.status.hidden = true
    } catch (error) {
      this.showStatus('Unable to download this file.')
      console.error(error)
    } finally {
      this.setActionsDisabled(false)
    }
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
    for (const menu of this.elements.entries.querySelectorAll<HTMLDetailsElement>('.entry-actions')) {
      menu.classList.toggle('is-disabled', disabled)

      const toggle = menu.querySelector<HTMLElement>('summary')
      toggle?.setAttribute('aria-disabled', String(disabled))
      
      if (disabled) {
        menu.open = false
      }
    }

    for (const button of this.elements.entries.querySelectorAll<HTMLButtonElement>('.entry-action')) {
      button.disabled = disabled
    }
  }

  // Close the file upload window explorer
  private closeActionMenus(restoreFocus = false) {
    for (const menu of this.elements.entries.querySelectorAll<HTMLDetailsElement>('.entry-actions[open]')) {
      menu.open = false
      if (restoreFocus) menu.querySelector<HTMLElement>('summary')?.focus()
    }
  }

  private joinPath(directory: string, name: string) {
    return directory === '/' ? `/${name}` : `${directory.replace(/\/$/, '')}/${name}`
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
      if (index > 0 && breadcrumbs[index - 1].label !== '/') {
        target.append(document.createTextNode(' / '))
      }

      const link = document.createElement('a')
      const url = new URL(window.location.href)
      
      url.searchParams.set('path', breadcrumb.path)
      link.href = url.toString()
      link.textContent = breadcrumb.label
      
      link.addEventListener('click', (event) => {
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
          return
        }

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
    
    entries.replaceChildren();

    // Render the ".." to go up in directory
    if (canGoUp && contents.path) {
      const button = document.createElement('button')
      
      button.type = 'button'
      button.textContent = '..'
      button.addEventListener('click', () => void this.load(getParentPath(contents.path!), 'push'))
      
      const item = document.createElement('li')
      
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

        if (entry.childCount != null) {
          const label = entry.childCount === 1 ? 'item' : 'items'
          entryMain.append(this.createEntryMetadata(`${entry.childCount} ${label}`))
        }
      } else {
        const name = document.createElement('span')

        name.textContent = entry.name ?? entry.path ?? '(unnamed file)'
        entryMain.append(name)

        if (entry.size != null) {
          entryMain.append(this.createEntryMetadata(formatFileSize(entry.size)))
        }
      }

      item.append(entryMain)

      if (entry.path) {
        const actions = document.createElement('details')
        actions.className = 'entry-actions'

        const toggle = document.createElement('summary')

        toggle.textContent = 'Actions'
        toggle.setAttribute('aria-label', `Actions for ${entry.name ?? entry.path}`)
        toggle.addEventListener('click', (event) => {
          if (actions.classList.contains('is-disabled')) {
            event.preventDefault()
            return
          }
          for (const menu of entries.querySelectorAll<HTMLDetailsElement>('.entry-actions[open]')) {
            if (menu !== actions) menu.open = false
          }
        })

        const menu = document.createElement('span')
        menu.className = 'entry-actions-menu'
        
        if (entry.type !== 'Directory') {
          menu.append(
            this.createAction(
              'Download',
              () => this.download(entry.path!, entry.name ?? 'download'),
              undefined,
              actions,
            ),
          )
        }
        menu.append(
          this.createAction('Move', () => this.move(entry.path!), undefined, actions),
          this.createAction('Copy', () => this.copy(entry.path!), undefined, actions),
          this.createAction('Delete', () => this.delete(entry.path!, entry.type === 'Directory'), 'danger', actions),
        )
        actions.append(toggle, menu)
        item.append(actions)
      }
      entries.append(item)
    }

    // Empty state
    if ((contents.entries?.length ?? 0) === 0) {
      const item = document.createElement('li');

      item.className = 'empty'
      item.textContent = 'This folder is empty.'
      
      entries.append(item)
    }
  }

  private createEntryMetadata(text: string) {
    const metadata = document.createElement('span')
    
    metadata.className = 'entry-metadata'
    metadata.textContent = text
    
    return metadata
  }

  private createAction(
    label: string,
    action: () => void,
    variant?: 'danger',
    menu?: HTMLDetailsElement,
  ) {
    const button = document.createElement('button')

    button.type = 'button'
    button.className = `entry-action${variant ? ` ${variant}` : ''}`
    button.textContent = label

    button.addEventListener('click', () => {
      if (menu) menu.open = false
      action()
    })

    return button
  }
}
