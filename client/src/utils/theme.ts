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
  function applyTheme(theme: Theme) {
    const isDark = theme === 'dark'
    document.documentElement.dataset.theme = theme
    button.textContent = isDark ? 'Light mode' : 'Dark mode'
    button.setAttribute('aria-pressed', String(isDark))
  }

  applyTheme(getInitialTheme())

  button.addEventListener('click', () => {
    const nextTheme: Theme =
      document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'

    localStorage.setItem(themeKey, nextTheme)
    applyTheme(nextTheme)
  })
}
