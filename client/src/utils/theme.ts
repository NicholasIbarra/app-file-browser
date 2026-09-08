const themeKey = 'theme'

type Theme = 'light' | 'dark'

function getInitialTheme(): Theme {
  const savedTheme = localStorage.getItem(themeKey)

  if (savedTheme === 'light' || savedTheme === 'dark') return savedTheme

  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light'
}

export function initializeThemeToggle(button: HTMLButtonElement) {
  applyTheme(getInitialTheme(), button)

  button.addEventListener('click', () => {
    const nextTheme: Theme =
      document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'

    localStorage.setItem(themeKey, nextTheme)
    applyTheme(nextTheme, button)
  })
}

function applyTheme(theme: Theme, button: HTMLButtonElement) {
    const isDark = theme === 'dark'
    document.documentElement.dataset.theme = theme

    const nextThemeLabel = isDark ? 'Light mode' : 'Dark mode'
    
    button.textContent = isDark ? '☀️' : '🌙'
    button.setAttribute('aria-label', nextThemeLabel)
    button.title = nextThemeLabel
    button.setAttribute('aria-pressed', String(isDark))
  }
