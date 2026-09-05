import { httpClient } from './../http-client.ts'
import type { paths } from './../generated/schema.ts'

type SearchOperation = paths['/api/search']['get']
type SearchQuery = NonNullable<SearchOperation['parameters']['query']>
type SearchResult =
  SearchOperation['responses'][200]['content']['application/json'][number]
type SemanticSearchOperation = paths['/api/search/semantic']['get']
type SemanticSearchQuery = NonNullable<SemanticSearchOperation['parameters']['query']>
type SemanticSearchResponse = SemanticSearchOperation['responses'][200]['content']['application/json']

export async function searchFilesSemantic(
  query: SemanticSearchQuery['query'],
  limit?: SemanticSearchQuery['limit'],
  signal?: AbortSignal,
): Promise<SemanticSearchResponse> {
  const response = await httpClient.get<SemanticSearchResponse>('/api/search/semantic', {
    params: limit === undefined ? { query } : { query, limit },
    signal,
  })
  return response.data
}

export async function searchFiles(
  query: NonNullable<SearchQuery['query']>,
  limit?: SearchQuery['limit'],
  signal?: AbortSignal,
): Promise<SearchResult[]> {
  const response = await httpClient.get<SearchResult[]>('/api/search', {
    params: limit === undefined ? { query } : { query, limit },
    signal,
  })

  return response.data
}

export type { SearchResult, SemanticSearchResponse }
