import { HubConnectionBuilder } from '@microsoft/signalr'
import { Notyf } from 'notyf'
import 'notyf/notyf.min.css'
import { apiBaseUrl } from './http-client'

export function connectFileHub(refresh: () => Promise<void>) {
  const notyf = new Notyf()
  const connection = new HubConnectionBuilder()
    .withUrl(`${apiBaseUrl.replace(/\/$/, '')}/hubs/files`, { withCredentials: false })
    .withAutomaticReconnect()
    .build()

  let disposed = false
  let retryTimer: ReturnType<typeof setTimeout> | undefined

  const refreshContents = () => {
    if (!disposed) {
      void refresh()
        .catch(error => console.error('Unable to refresh files.', error))
    }
  }

  connection.on('FileUploadCompleted', (_path: string) => {
    notyf.success('File uploaded successfully.')
    refreshContents()
  })

  connection.onreconnected(refreshContents)
  connection.onclose(() => {
    if (!disposed) {
      void start()
    }
  })

  async function start() {
    try {
      await connection.start()
      refreshContents()
    } catch (error) {
      if (!disposed) {
        console.error('Unable to connect to file updates. Retrying…', error)
        retryTimer = setTimeout(() => void start(), 5000)
      }
    }
  }

  void start()

  return () => {
    disposed = true
    clearTimeout(retryTimer)
    void connection.stop().catch(error => console.error('Unable to stop file updates.', error))
  }
}
