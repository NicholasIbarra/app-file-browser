// @vitest-environment jsdom

import { beforeEach, describe, expect, it, vi } from 'vitest'
import { initializeThemeToggle } from './theme'

function mockSystemTheme(prefersDark: boolean) {
  Object.defineProperty(window, 'matchMedia', {
    configurable: true,
    value: vi.fn().mockReturnValue({ matches: prefersDark }),
  })
}

describe('initializeThemeToggle', () => {
  beforeEach(() => {
    localStorage.clear()
    delete document.documentElement.dataset.theme
    document.body.innerHTML = '<button type="button"></button>'
    mockSystemTheme(false)
  })

  it('uses the system preference when no theme has been saved', () => {
    mockSystemTheme(true)
    const button = document.querySelector('button')!

    initializeThemeToggle(button)

    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(button.textContent).toBe('Light mode')
    expect(button.getAttribute('aria-pressed')).toBe('true')
  })

  it('prefers a saved theme over the system preference', () => {
    localStorage.setItem('theme', 'light')
    mockSystemTheme(true)
    const button = document.querySelector('button')!

    initializeThemeToggle(button)

    expect(document.documentElement.dataset.theme).toBe('light')
    expect(button.textContent).toBe('Dark mode')
  })

  it('toggles and persists the selected theme', () => {
    const button = document.querySelector('button')!
    initializeThemeToggle(button)

    button.click()

    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(button.textContent).toBe('Light mode')
    expect(button.getAttribute('aria-pressed')).toBe('true')
  })
})
