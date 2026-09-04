import { httpClient } from './../http-client.ts'
import type { components, paths } from './../generated/schema.ts'

type GetDirectoryOperation = paths['/api/directories']['get']
type GetDirectoryQuery = NonNullable<
  GetDirectoryOperation['parameters']['query']
>
type DirectoryContents =
  GetDirectoryOperation['responses'][200]['content']['application/json']
type FileOperationRequest = components['schemas']['FileOperationRequest']

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

export async function uploadFile(
  path: string,
  file: File,
  overwrite = false,
): Promise<void> {
  const form = new FormData()
  form.append('file', file)
  await httpClient.post('/api/directories/upload', form, {
    params: { path, overwrite },
  })
}

export async function deleteEntry(
  path: string,
  recursive = false,
): Promise<void> {
  await httpClient.delete('/api/directories', {
    params: { path, recursive },
  })
}

export async function moveEntry(request: FileOperationRequest): Promise<void> {
  await httpClient.post('/api/directories/move', request)
}

export async function copyEntry(request: FileOperationRequest): Promise<void> {
  await httpClient.post('/api/directories/copy', request)
}

export type { DirectoryContents, FileOperationRequest }
