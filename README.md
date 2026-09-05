# File Browser

## Overview

A file browser with an ASP.NET Core API and TypeScript UI. Browse folders, upload, download, copy, move, and delete files across local storage or Azure Blob Storage. Search names and paths with ranked text matching or AI-assisted semantic search.

## Browser service

- [DirectoryBrowserService](src/FileBrowser.Application/Browsing/DirectoryBrowserService.cs) lists directory contents, treats an empty path as root, and sorts folders first, then names alphabetically.
- [FileService](src/FileBrowser.Application/Files/FileService.cs) handles downloads and file operations. Uploads run through a Hangfire background job; successful writes publish MediatR events.
- [IFileSystem](src/FileBrowser.Application/Abtractstions/FileSystem/IFileSystem.cs) is the shared contract for listing, reading, and modifying storage. Browsing, file operations, and indexing all use this abstraction.

[Provider registration](src/FileBrowser.Infrastructure/DependencyInjection.cs) selects the implementation through `FileBrowser:Provider`:

| Provider | Implementation | Storage root |
| --- | --- | --- |
| `Local` | [LocalFileSystem](src/FileBrowser.Infrastructure/FileSystem/LocalFileSystem.cs) | `FileBrowser:HomeDirectory` |
| `Azure` | [AzureFileSystem](src/FileBrowser.Infrastructure/FileSystem/AzureFileSystem.cs) | Blob container with an optional prefix; folders are represented by blob paths |

Additional file systems can implement `IFileSystem` and register a provider while reusing the application services.

## Indexing

At startup, [FileIndexHostedService](src/FileBrowser.Infrastructure/BackgroundServices/FileIndexHostedService.cs) calls `RebuildAsync` on [FileSearchIndex](src/FileBrowser.Infrastructure/Indexing/FileSearchIndex.cs):

1. Recursively enumerate files and folders through `IFileSystem`.
2. Build an in-memory index of names, paths, entry types, and extensions. Generate metadata embeddings when `OpenAI:AzureOpenAI:Enabled` is enabled.
3. Atomically replace the search snapshot. Readers keep using the previous snapshot until the replacement is ready.

The index is rebuilt on application startup and is not persisted.

[FileSearchScorer](src/FileBrowser.Application/Search/Scorer/FileSearchScorer.cs) uses the first matching rule, ignoring case:

| Match | Score |
| --- | ---: |
| Exact name | 1000 |
| Name prefix | 800 |
| Word prefix within name | 600 |
| Name contains query | 400 |
| Path contains query | 200 |

Text search ranks by score, then shorter names, and returns up to the requested limit (default 50).

## Index updates

Successful file operations publish events; handlers update only the affected paths and descendants. Updates are serialized and publish a complete replacement snapshot. Upload events are published after the background job writes the file.

There is no storage watcher or periodic refresh. External storage changes require a rebuild. Incremental additions, copies, and moves currently create entries without embeddings, so those entries become available to semantic search after a full rebuild.

| Event | Handler | Effect |
| --- | --- | --- |
| `FileCreatedEvent` | [FileCreatedEventHandler](src/FileBrowser.Application/Files/EventHandlers/FileCreatedEventHandler.cs) | Add or replace the uploaded path |
| `FileCreatedEvent` | [FileUploadCompletedEventHandler](src/FileBrowser.Api/Hubs/FileUploadCompletedEventHandler.cs) | Broadcast `FileUploadCompleted` through SignalR; the UI refreshes |
| `FileCopiedEvent` | [FileCopiedEventHandler](src/FileBrowser.Application/Files/EventHandlers/FileCopiedEventHandler.cs) | Add or replace the destination subtree |
| `FileMovedEvent` | [FileMovedEventHandler](src/FileBrowser.Application/Files/EventHandlers/FileMovedEventHandler.cs) | Remove the source and replace the destination subtree |
| `FileDeletedEvent` | [FileDeletedEventHandler](src/FileBrowser.Application/Files/EventHandlers/FileDeletedEventHandler.cs) | Remove the path and descendants |

## AI search

[FileSearchService](src/FileBrowser.Application/Search/FileSearchService.cs) coordinates three steps using Azure OpenAI:

1. **Normalize the query.** A chat prompt rewrites the request into concise search terms, preserving explicit names, extensions, and constraints. The response is trimmed; an empty response falls back to the original query.
2. **Embed and rank.** Embed those terms and compare the vector against indexed metadata embeddings using [cosine similarity](src/FileBrowser.Application/Search/Semantic/CosineSimilarity.cs): `dot(query, entry) / (length(query) * length(entry))`. Higher scores rank first, with paths breaking ties. Return the top results up to the limit; there is no minimum similarity threshold.
3. **Summarize.** Send the original query and selected result metadata to the chat model for a brief explanation. The [summary prompt](src/FileBrowser.Application/Search/Prompts/FileSearchPromptBuilder.cs) keeps the selected results unchanged. An empty response or summary failure falls back to a file/folder count.

Semantic search uses names, paths, types, and extensions. File contents are not indexed or sent to the summarizer.

## UI

The [client](client) uses TypeScript, Vite, and direct DOM components. [main.ts](client/src/main.ts) connects the app shell, directory explorer, search dialog, navigation, theme, and live upload updates.

| Location | Responsibility |
| --- | --- |
| [api/generated/](client/src/api/generated) | TypeScript types generated from the API's OpenAPI schema |
| [api/services/](client/src/api/services) | Typed directory operations, text/semantic search, and settings requests |
| [api/http-client.ts](client/src/api/http-client.ts) | Shared Axios client; API address set by `VITE_API_BASE_URL` |
| [api/file-hub.ts](client/src/api/file-hub.ts) | SignalR connection, upload notifications, and directory refresh |
| [components/](client/src/components) | App shell, directory browsing/file actions, and search dialog |
| [utils/](client/src/utils) | Navigation, themes, notifications, and file-size formatting |
| [style.css](client/src/style.css) | Shared layout and styles |

After API contract changes, run `npm run generate:api-types` from `client/` with the API running. The generator reads `https://localhost:7246/swagger/v1/swagger.json` by default (`OPENAPI_SCHEMA_URL` overrides it). Services derive request and response types from the generated schema; components use those services.
