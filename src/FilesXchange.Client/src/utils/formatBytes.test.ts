import { formatBytes } from './formatBytes'

describe('formatBytes', () => {
  it('returns bytes for zero', () => {
    expect(formatBytes(0)).toBe('0 B')
  })

  it('formats bytes without decimals for whole bytes', () => {
    expect(formatBytes(512)).toBe('512 B')
  })

  it('formats kilobytes with one decimal below ten', () => {
    expect(formatBytes(1536)).toBe('1.5 KB')
  })

  it('formats megabytes without decimals at ten or more', () => {
    expect(formatBytes(10 * 1024 * 1024)).toBe('10 MB')
  })

  it('formats gigabytes using the largest matching unit', () => {
    expect(formatBytes(3 * 1024 * 1024 * 1024)).toBe('3.0 GB')
  })
})
