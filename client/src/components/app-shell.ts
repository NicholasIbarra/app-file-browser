export interface AppShell {
  entries: HTMLUListElement
  path: HTMLParagraphElement
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
      <header>
        <h1>File Explorer</h1>
        
        <div class="header-actions">
          <button id="upload-button" type="button">Upload</button>
          <input id="upload-input" class="visually-hidden" type="file" multiple />
          <button id="search-button" type="button" aria-haspopup="dialog"><span>Search</span><kbd aria-hidden="true">⌘ K</kbd></button>
          <button id="theme-toggle" type="button"></button>
        </div>
      </header>

      <p id="current-path" aria-live="polite"></p>
      <p id="status" role="status">Loading…</p>
      <ul id="entries" aria-label="Directory contents"></ul>
    
    </main>
    
    <div id="search-overlay" class="search-overlay" hidden>
      <section class="search-dialog" role="dialog" aria-modal="true" aria-labelledby="search-title">
        <h2 id="search-title" class="visually-hidden">Search files</h2>
        <div class="search-tabs" role="tablist" aria-label="Search mode">
          <button id="file-search-tab" type="button" role="tab" aria-selected="true" aria-controls="file-search-panel">Search</button>
          <button id="ai-search-tab" type="button" role="tab" aria-selected="false" aria-controls="ai-search-panel" tabindex="-1">AI Search</button>
        </div>
        <div id="file-search-panel" role="tabpanel" aria-labelledby="file-search-tab">
        <div class="search-field">
          <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z" /></svg>
          <input id="search-input" type="search" aria-label="Search files and folders" placeholder="Search files and folders" autocomplete="off" spellcheck="false" />
          <kbd>Esc</kbd>
        </div>
        <p id="search-status" class="search-hint" role="status">Start typing to search your files.</p>
        <ul id="search-results" aria-label="Search results" hidden></ul>
        </div>
        <div id="ai-search-panel" role="tabpanel" aria-labelledby="ai-search-tab" hidden>
          <form id="ai-search-form" class="search-field">
            <svg class="ai-search-icon" aria-hidden="true" viewBox="0 0 24 24"><path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5L12 3ZM20 2v4m-2-2h4" /></svg>
            <input id="ai-search-input" type="search" aria-label="Describe the files you need" aria-describedby="ai-search-help" placeholder="Describe the files you need…" autocomplete="off" required />
            <button class="ai-search-submit" type="submit" aria-label="Submit AI search">Ask <span aria-hidden="true">↵</span></button>
          </form>
          <div class="ai-search-content">
            <p id="ai-search-help">Find files by describing what you remember.</p>
            <div class="ai-search-examples" aria-label="Example searches">
              <button type="button">Find invoices from last month</button>
              <button type="button">Photos from our team offsite</button>
              <button type="button">The latest project proposal PDF</button>
            </div>
            <div id="ai-search-preview" class="ai-search-preview" role="status" hidden></div>
          </div>
          <ul id="ai-search-results" aria-label="AI search results" hidden></ul>
        </div>
      </section>
    </div>`

  return {
    entries: getElement(root, '#entries'), 
    path: getElement(root, '#current-path'),
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
