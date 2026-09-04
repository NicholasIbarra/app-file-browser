// @vitest-environment jsdom

import { beforeEach, describe, expect, it } from 'vitest'
import {
  getParentPath,
  getPathBreadcrumbs,
  getUrlPath,
  updateUrl,
} from './navigation'

describe('navigation', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/')
  })

  it('reads a directory path from the URL', () => {
    window.history.replaceState({}, '', '/?path=C%3A%5CProjects%5Capp')

    expect(getUrlPath()).toBe('C:\\Projects\\app')
  })

  it('returns undefined when the URL has no directory path', () => {
    expect(getUrlPath()).toBeUndefined()
  })

  it('updates and removes the path query parameter', () => {
    window.history.replaceState({}, '', '/?view=list')

    updateUrl('/projects/app', 'push')
    expect(window.location.search).toBe('?view=list&path=%2Fprojects%2Fapp')

    updateUrl(undefined, 'replace')
    expect(window.location.search).toBe('?view=list')
  })

  it.each([
    ['/projects/app/src', '/projects/app'],
    ['/projects', '/'],
    ['C:\\Projects\\app', 'C:\\Projects'],
    ['C:\\Projects', 'C:\\'],
    ['\\\\server\\share\\folder', '\\\\server\\share'],
    ['folder/subfolder/', 'folder'],
  ])('finds the parent of %s', (path, parent) => {
    expect(getParentPath(path)).toBe(parent)
  })

  it.each(['/', 'C:\\', '\\\\server\\share', 'folder'])(
    'does not navigate above the root path %s',
    (path) => {
      expect(getParentPath(path)).toBeUndefined()
    },
  )

  it('builds links for each segment of a Unix path', () => {
    expect(getPathBreadcrumbs('/obj/Debug/net10.0')).toEqual([
      { label: '/', path: '/' },
      { label: 'obj', path: '/obj' },
      { label: 'Debug', path: '/obj/Debug' },
      { label: 'net10.0', path: '/obj/Debug/net10.0' },
    ])
  })

  it('builds links for each segment of a Windows path', () => {
    expect(getPathBreadcrumbs('C:\\Projects\\app')).toEqual([
      { label: 'C:', path: 'C:\\' },
      { label: 'Projects', path: 'C:\\Projects' },
      { label: 'app', path: 'C:\\Projects\\app' },
    ])
  })
})
