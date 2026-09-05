import { httpClient } from './../http-client.ts'
import type { components, paths } from './../generated/schema.ts'

type DownloadFileOperation = paths['/api/files/download']['get']
type DownloadFileQuery = NonNullable<
  DownloadFileOperation['parameters']['query']
>
type FileOperationRequest = components['schemas']['FileOperationRequest']

export async function downloadFile(
  path: NonNullable<DownloadFileQuery['path']>,
): Promise<Blob> {
  const response = await httpClient.get<Blob>('/api/files/download', {
    params: { path },
    responseType: 'blob',
  })

  return response.data
}

export async function uploadFile(
  path: string,
  file: File,
  overwrite = false,
): Promise<void> {
  const form = new FormData()

  form.append('file', file)

  await httpClient.post('/api/files/upload', form, {
    params: { path, overwrite },
  })
}

export async function deleteEntry(
  path: string,
  recursive = false,
): Promise<void> {
  await httpClient.delete('/api/files', {
    params: { path, recursive },
  })
}

export async function moveEntry(request: FileOperationRequest): Promise<void> {
  await httpClient.post('/api/files/move', request)
}

export async function copyEntry(request: FileOperationRequest): Promise<void> {
  await httpClient.post('/api/files/copy', request)
}

export type { FileOperationRequest }
