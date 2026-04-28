import { fireEvent, render, screen } from '@testing-library/react'
import { UploadPage } from './UploadPage'

describe('UploadPage', () => {
  it('shows an inline error and disables upload when selected files exceed 2 GB', () => {
    render(<UploadPage />)

    const oversizedFile = createFileWithSize('large.bin', 2 * 1024 * 1024 * 1024 + 1)

    fireEvent.change(screen.getByLabelText(/drop files here/i), {
      target: { files: [oversizedFile] },
    })

    expect(
      screen.getByText(
        'Total upload size must not exceed 2 GB. Remove one or more files.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Upload files' })).toBeDisabled()
  })
})

function createFileWithSize(name: string, size: number): File {
  const file = new File(['content'], name, { type: 'application/octet-stream' })

  Object.defineProperty(file, 'size', {
    value: size,
  })

  return file
}
