// @vitest-environment jsdom

import { beforeEach, expect, it, vi } from 'vitest'
import { getDirectory, type DirectoryContents } from '../api'
import { renderAppShell } from './app-shell'
import { DirectoryExplorer } from './directory-explorer'

vi.mock('../api', () => ({
  getDirectory: vi.fn(),
  copyEntry: vi.fn(),
  deleteEntry: vi.fn(),
  downloadFile: vi.fn(),
  moveEntry: vi.fn(),
  uploadFile: vi.fn(),
}))

beforeEach(() => vi.resetAllMocks())

it('refreshes the current directory without changing browser history', async () => {
  const shell = renderAppShell(document.createElement('div'))
  const explorer = new DirectoryExplorer(shell)
  vi.mocked(getDirectory).mockResolvedValue({ path: '/docs', entries: [] })
  await explorer.load('/docs', 'push')
  const url = window.location.href
  vi.mocked(getDirectory).mockResolvedValue({
    path: '/docs', entries: [{ name: 'uploaded.txt', path: '/docs/uploaded.txt', type: 'File' }],
  })

  await explorer.refresh()

  expect(getDirectory).toHaveBeenLastCalledWith('/docs')
  expect(shell.entries.textContent).toContain('uploaded.txt')
  expect(window.location.href).toBe(url)
})

it('keeps the destination visible when a refresh races with navigation', async () => {
  const shell = renderAppShell(document.createElement('div'))
  const explorer = new DirectoryExplorer(shell)
  let completeNavigation!: (contents: DirectoryContents) => void
  vi.mocked(getDirectory).mockReturnValueOnce(new Promise(resolve => { completeNavigation = resolve }))
  const navigation = explorer.load('/docs', 'push')
  vi.mocked(getDirectory).mockResolvedValueOnce({ path: '/docs', entries: [{ name: 'new.txt' }] })

  await explorer.refresh()
  completeNavigation({ path: '/docs', entries: [] })
  await navigation

  expect(getDirectory).toHaveBeenLastCalledWith('/docs')
  expect(shell.entries.textContent).toContain('new.txt')
})
