import { describe, expect, it } from 'vitest'
import { formatFileSize } from './file-size'

describe('formatFileSize', () => {
  it.each([
    [0, '0 B'],
    [1, '1 B'],
    [1023, '1,023 B'],
    [1024, '1 KB'],
    [1536, '1.5 KB'],
    [1024 ** 2, '1 MB'],
    [2.5 * 1024 ** 3, '2.5 GB'],
    [1024 ** 4, '1 TB'],
  ])('formats %d bytes as %s', (bytes, expected) => {
    expect(formatFileSize(bytes)).toBe(expected)
  })

  it('caps the displayed unit at terabytes', () => {
    expect(formatFileSize(1024 ** 5)).toBe('1,024 TB')
  })
})
