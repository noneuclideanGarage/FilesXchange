import { fireEvent, render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { FileList } from './FileList'

describe('FileList', () => {
  it('renders an empty state when there are no files', () => {
    render(<FileList files={[]} onRemoveFile={vi.fn()} />)

    expect(
      screen.getByText('Selected files will appear here before upload.'),
    ).toBeInTheDocument()
  })

  it('renders file names and formatted sizes', () => {
    const files = [
      new File(['a'.repeat(1536)], 'report.txt', { type: 'text/plain' }),
      new File(['b'.repeat(512)], 'notes.txt', { type: 'text/plain' }),
    ]

    render(<FileList files={files} onRemoveFile={vi.fn()} />)

    expect(screen.getByText('report.txt')).toBeInTheDocument()
    expect(screen.getByText('notes.txt')).toBeInTheDocument()
    expect(screen.getByText('1.5 KB')).toBeInTheDocument()
    expect(screen.getByText('512 B')).toBeInTheDocument()
  })

  it('calls onRemoveFile with the correct index', () => {
    const onRemoveFile = vi.fn()
    const files = [
      new File(['first'], 'first.txt', { type: 'text/plain' }),
      new File(['second'], 'second.txt', { type: 'text/plain' }),
    ]

    render(<FileList files={files} onRemoveFile={onRemoveFile} />)

    fireEvent.click(screen.getByRole('button', { name: 'Remove second.txt' }))

    expect(onRemoveFile).toHaveBeenCalledWith(1)
  })

  it('disables removal buttons when disabled', () => {
    const files = [new File(['first'], 'first.txt', { type: 'text/plain' })]

    render(<FileList disabled files={files} onRemoveFile={vi.fn()} />)

    expect(
      screen.getByRole('button', { name: 'Remove first.txt' }),
    ).toBeDisabled()
  })
})
