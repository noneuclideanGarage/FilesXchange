import { fireEvent, render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import type { UploadResponse } from '../../api/client'
import { ResultCard } from './ResultCard'

describe('ResultCard', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders the upload result and shareable link', () => {
    const shareUrl = `${window.location.origin}/download/upload-token`

    render(<ResultCard result={createResult()} onReset={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent('Your link is ready')
    expect(screen.getByText('upload-token')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: shareUrl })).toHaveAttribute(
      'href',
      shareUrl,
    )
    expect(screen.getByRole('button', { name: 'Copy link' })).toBeInTheDocument()
  })

  it('copies the shareable frontend link', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined)
    const shareUrl = `${window.location.origin}/download/upload-token`

    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: {
        writeText,
      },
    })

    render(<ResultCard result={createResult()} onReset={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Copy link' }))

    expect(writeText).toHaveBeenCalledWith(shareUrl)
    expect(await screen.findByRole('button', { name: 'Copied' })).toBeInTheDocument()
  })

  it('renders singular file count text', () => {
    render(
      <ResultCard
        result={createResult({ fileCount: 1 })}
        onReset={vi.fn()}
      />,
    )

    expect(screen.getByText(/1 file available until/i)).toBeInTheDocument()
  })

  it('calls onReset when sharing more files', () => {
    const onReset = vi.fn()

    render(<ResultCard result={createResult()} onReset={onReset} />)

    fireEvent.click(screen.getByRole('button', { name: 'Share more' }))

    expect(onReset).toHaveBeenCalledOnce()
  })
})

function createResult(overrides: Partial<UploadResponse> = {}): UploadResponse {
  return {
    token: 'upload-token',
    fileCount: 2,
    expiresAt: '2026-04-25T00:00:00Z',
    downloadUrl: '/api/download/upload-token',
    ...overrides,
  }
}
