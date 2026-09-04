import axios from 'axios'

const apiBaseUrl =
  window.__API_BASE_URL__ ||
  import.meta.env.VITE_API_BASE_URL ||
  'https://localhost:7246'

export const httpClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    Accept: 'application/json',
  },
})
