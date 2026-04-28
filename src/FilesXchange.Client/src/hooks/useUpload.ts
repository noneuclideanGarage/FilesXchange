import { useState } from 'react'
import {
  ApiClientError,
  type UploadProgress,
  type UploadResponse,
  uploadFiles,
} from '../api/client'

type UploadState = 'idle' | 'uploading' | 'success' | 'error'

type UseUploadResult = {
  error: ApiClientError | null
  progress: UploadProgress | null
  resetUpload: () => void
  result: UploadResponse | null
  startUpload: (files: readonly File[]) => Promise<void>
  status: UploadState
}

export function useUpload(): UseUploadResult {
  const [status, setStatus] = useState<UploadState>('idle')
  const [progress, setProgress] = useState<UploadProgress | null>(null)
  const [result, setResult] = useState<UploadResponse | null>(null)
  const [error, setError] = useState<ApiClientError | null>(null)

  async function startUpload(files: readonly File[]) {
    setStatus('uploading')
    setProgress(null)
    setResult(null)
    setError(null)

    try {
      const uploadResult = await uploadFiles(files, setProgress)
      setResult(uploadResult)
      setStatus('success')
    } catch (caughtError) {
      setError(normalizeUploadError(caughtError))
      setStatus('error')
    }
  }

  function resetUpload() {
    setStatus('idle')
    setProgress(null)
    setResult(null)
    setError(null)
  }

  return {
    error,
    progress,
    resetUpload,
    result,
    startUpload,
    status,
  }
}

function normalizeUploadError(error: unknown): ApiClientError {
  if (error instanceof ApiClientError) {
    return error
  }

  return new ApiClientError({
    kind: 'unknown',
    code: 'unexpected_error',
    message: 'Unexpected upload error.',
  })
}
