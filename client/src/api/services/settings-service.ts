import { httpClient } from '../http-client.ts'
import type { paths } from '../generated/schema.ts'

export type SettingsResponse = paths['/api/settings']['get']['responses'][200]['content']['application/json']

export async function getSettings(): Promise<SettingsResponse> {
  const response = await httpClient.get<SettingsResponse>('/api/settings')
  return response.data
}
