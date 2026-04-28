import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import { ApiClientError, getInfo } from '../api/client'
import { DownloadPage } from './DownloadPage'

vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client')

  return {
    ...actual,
    getInfo: vi.fn(),
  }
})

const getInfoMock = vi.mocked(getInfo)

describe('DownloadPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('renders not found state for 404 responses', async () => {
    getInfoMock.mockRejectedValue(
      new ApiClientError({
        kind: 'not-found',
        status: 404,
        message: 'The requested token was not found.',
      }),
    )

    renderDownloadPage()

    expect(await screen.findByText('Link not found')).toBeInTheDocument()
    expect(
      screen.getByText('This token does not match an active file exchange.'),
    ).toBeInTheDocument()
  })

  it('renders expired state for 410 responses', async () => {
    getInfoMock.mockRejectedValue(
      new ApiClientError({
        kind: 'gone',
        status: 410,
        message: 'The requested token has expired.',
      }),
    )

    renderDownloadPage()

    expect(await screen.findByText('Link expired')).toBeInTheDocument()
    expect(
      screen.getByText('This file exchange is no longer available.'),
    ).toBeInTheDocument()
  })

  it('renders server error state for 500 responses', async () => {
    getInfoMock.mockRejectedValue(
      new ApiClientError({
        kind: 'server',
        status: 500,
        message: 'Server error. Try again later.',
      }),
    )

    renderDownloadPage()

    expect(await screen.findByText('Server problem')).toBeInTheDocument()
    expect(
      screen.getByText('The server could not load this file exchange. Try again later.'),
    ).toBeInTheDocument()
  })
})

function renderDownloadPage() {
  render(
    <MemoryRouter initialEntries={['/download/download-token']}>
      <Routes>
        <Route path="/download/:token" element={<DownloadPage />} />
      </Routes>
    </MemoryRouter>,
  )
}
