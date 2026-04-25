import { act, renderHook } from '@testing-library/react'
import { vi } from 'vitest'
import {
  ApiClientError,
  type UploadResponse,
  uploadFiles,
} from '../api/client'
import { useUpload } from './useUpload'

vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client')

  return {
    ...actual,
    uploadFiles: vi.fn(),
  }
})

const uploadFilesMock = vi.mocked(uploadFiles)

describe('useUpload', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('transitions to success and stores result after upload completes', async () => {
    const files = [new File(['hello'], 'hello.txt', { type: 'text/plain' })]
    const response: UploadResponse = {
      token: 'upload-token',
      fileCount: 1,
      expiresAt: '2026-04-25T00:00:00Z',
      downloadUrl: '/api/download/upload-token',
    }

    uploadFilesMock.mockResolvedValue(response)

    const { result } = renderHook(() => useUpload())

    await act(async () => {
      await result.current.startUpload(files)
    })

    expect(uploadFilesMock).toHaveBeenCalledWith(files, expect.any(Function))
    expect(result.current.status).toBe('success')
    expect(result.current.result).toEqual(response)
    expect(result.current.error).toBeNull()
  })

  it('transitions to error and stores api error when upload fails', async () => {
    const files = [new File(['hello'], 'hello.txt', { type: 'text/plain' })]
    const error = new ApiClientError({
      kind: 'network',
      code: 'network_error',
      message: 'Network error. Check your connection.',
    })

    uploadFilesMock.mockRejectedValue(error)

    const { result } = renderHook(() => useUpload())

    await act(async () => {
      await result.current.startUpload(files)
    })

    expect(result.current.status).toBe('error')
    expect(result.current.error).toBe(error)
    expect(result.current.result).toBeNull()
  })

  it('resetUpload returns state to idle and clears transient data', async () => {
    const files = [new File(['hello'], 'hello.txt', { type: 'text/plain' })]
    const response: UploadResponse = {
      token: 'upload-token',
      fileCount: 1,
      expiresAt: '2026-04-25T00:00:00Z',
      downloadUrl: '/api/download/upload-token',
    }

    uploadFilesMock.mockImplementation(async (_files, onProgress) => {
      onProgress?.({
        loadedBytes: 5,
        totalBytes: 5,
        percent: 100,
      })

      return response
    })

    const { result } = renderHook(() => useUpload())

    await act(async () => {
      await result.current.startUpload(files)
    })

    act(() => {
      result.current.resetUpload()
    })

    expect(result.current.status).toBe('idle')
    expect(result.current.progress).toBeNull()
    expect(result.current.result).toBeNull()
    expect(result.current.error).toBeNull()
  })
})
