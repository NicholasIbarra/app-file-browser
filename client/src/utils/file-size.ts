const units = ['B', 'KB', 'MB', 'GB', 'TB']

export function formatFileSize(bytes: number) {
  if (bytes === 0) return '0 B'

  const unitIndex = Math.min(
    Math.floor(Math.log(Math.max(bytes, 1)) / Math.log(1024)),
    units.length - 1,
  )
  const value = bytes / 1024 ** unitIndex

  return `${new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 }).format(value)} ${units[unitIndex]}`
}
