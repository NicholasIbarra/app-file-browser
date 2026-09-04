import { httpClient } from './../http-client.ts'
import type { paths } from './../generated/schema.ts'

type GetDirectoryOperation = paths['/api/directories']['get']
type GetDirectoryQuery = NonNullable<
  GetDirectoryOperation['parameters']['query']
>
type DirectoryContents =
  GetDirectoryOperation['responses'][200]['content']['application/json']

export async function getDirectory(
  path?: GetDirectoryQuery['path'],
): Promise<DirectoryContents> {
  const response = await httpClient.get<DirectoryContents>('/api/directories', {
    params: path === undefined ? undefined : { path },
  })

  return response.data
}

export async function reIndexDirectories(): Promise<void> {
  await httpClient.post('/api/directories/re-index')
}

export type { DirectoryContents }
