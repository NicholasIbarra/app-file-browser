export interface AppShell {
  entries: HTMLUListElement
  path: HTMLParagraphElement
  // reIndexButton: HTMLButtonElement | undefined
  uploadButton: HTMLButtonElement
  uploadInput: HTMLInputElement
  searchButton: HTMLButtonElement
  searchInput: HTMLInputElement
  searchOverlay: HTMLDivElement
  searchResults: HTMLUListElement
  searchStatus: HTMLParagraphElement
  status: HTMLParagraphElement
  themeToggle: HTMLButtonElement
}

function getElement<T extends Element>(root: ParentNode, selector: string): T {
  const element = root.querySelector<T>(selector)
  if (!element) throw new Error(`App shell is missing ${selector}`)
  return element
}

export function renderAppShell(root: HTMLDivElement): AppShell {
  root.innerHTML = `
    <main class="explorer">
      <header><h1>File Explorer</h1><div class="header-actions">
        <button id="upload-button" type="button">Upload</button>
        <input id="upload-input" class="visually-hidden" type="file" multiple />
        <!-- <button id="re-index-button" type="button">Re-index</button> -->
        <button id="search-button" type="button" aria-haspopup="dialog"><span>Search</span><kbd aria-hidden="true">⌘ K</kbd></button>
        <button id="theme-toggle" type="button"></button>
      </div></header>
      <p id="current-path" aria-live="polite"></p>
      <p id="status" role="status">Loading…</p>
      <ul id="entries" aria-label="Directory contents"></ul>
    </main>
    <div id="search-overlay" class="search-overlay" hidden>
      <section class="search-dialog" role="dialog" aria-modal="true" aria-labelledby="search-title">
        <h2 id="search-title" class="visually-hidden">Search files</h2>
        <div class="search-field">
          <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z" /></svg>
          <input id="search-input" type="search" placeholder="Search files and folders" autocomplete="off" spellcheck="false" />
          <kbd>Esc</kbd>
        </div>
        <p id="search-status" class="search-hint" role="status">Start typing to search your files.</p>
        <ul id="search-results" aria-label="Search results" hidden></ul>
      </section>
    </div>`

  return {
    entries: getElement(root, '#entries'), path: getElement(root, '#current-path'),
    // reIndexButton: getElement(root, '#re-index-button'),
    searchButton: getElement(root, '#search-button'),
    uploadButton: getElement(root, '#upload-button'), 
    uploadInput: getElement(root, '#upload-input'),
    searchInput: getElement(root, '#search-input'),
     searchOverlay: getElement(root, '#search-overlay'),
    searchResults: getElement(root, '#search-results'), 
    searchStatus: getElement(root, '#search-status'),
    status: getElement(root, '#status'), 
    themeToggle: getElement(root, '#theme-toggle'),
  }
}
