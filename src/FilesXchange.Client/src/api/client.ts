export type UploadProgress = {
  loadedBytes: number
  totalBytes: number | null
  percent: number | null
}

export type UploadResponse = {
  token: string
  fileCount: number
  expiresAt: string
  downloadUrl: string
}

export type FileInfoResponse = {
  token: string
  fileCount: number
  expiresAt: string
  fileNames: string[]
  downloadUrl: string
}

export type ApiClientErrorKind =
  | 'bad-request'
  | 'payload-too-large'
  | 'not-found'
  | 'gone'
  | 'network'
  | 'server'
  | 'unknown'

export class ApiClientError extends Error {
  readonly kind: ApiClientErrorKind
  readonly status: number | null
  readonly code: string

  constructor(params: {
    kind: ApiClientErrorKind
    message: string
    status?: number | null
    code?: string
  }) {
    super(params.message)
    this.name = 'ApiClientError'
    this.kind = params.kind
    this.status = params.status ?? null
    this.code = params.code ?? params.kind
  }
}

type ErrorEnvelope = {
  error?: {
    code?: string
    message?: string
  }
}

type UploadResponseDto = {
  token?: string
  Token?: string
  fileCount?: number
  FileCount?: number
  expiresAt?: string
  ExpiresAt?: string
  downloadUrl?: string
  DownloadUrl?: string
}

type FileInfoResponseDto = UploadResponseDto & {
  fileNames?: string[]
  FileNames?: string[]
}

const API_BASE_URL = '/api'

export function uploadFiles(
  files: readonly File[],
  onProgress?: (progress: UploadProgress) => void,
): Promise<UploadResponse> {
  if (files.length === 0) {
    return Promise.reject(
      new ApiClientError({
        kind: 'bad-request',
        status: 400,
        code: 'files_missing',
        message: 'At least one file must be selected.',
      }),
    )
  }

  const formData = new FormData()
  for (const file of files) {
    formData.append('files', file)
  }

  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest()

    request.open('POST', `${API_BASE_URL}/upload`)
    request.responseType = 'text'

    request.upload.onprogress = (event) => {
      if (!onProgress) {
        return
      }

      const totalBytes = event.lengthComputable ? event.total : null
      const percent =
        totalBytes === null ? null : Math.round((event.loaded / totalBytes) * 100)

      onProgress({
        loadedBytes: event.loaded,
        totalBytes,
        percent,
      })
    }

    request.onload = () => {
      const responseBody = parseJson(request.responseText)

      if (request.status >= 200 && request.status < 300) {
        resolve(normalizeUploadResponse(responseBody))
        return
      }

      reject(createApiError(request.status, responseBody))
    }

    request.onerror = () => {
      reject(createNetworkError())
    }

    request.onabort = () => {
      reject(createNetworkError('Upload request was aborted.'))
    }

    request.send(formData)
  })
}

export async function getInfo(token: string): Promise<FileInfoResponse> {
  const response = await requestJson(`${API_BASE_URL}/info/${encodeURIComponent(token)}`)
  return normalizeFileInfoResponse(response)
}

async function requestJson(url: string): Promise<unknown> {
  let response: Response

  try {
    response = await fetch(url)
  } catch {
    throw createNetworkError()
  }

  const responseBody = parseJson(await response.text())

  if (!response.ok) {
    throw createApiError(response.status, responseBody)
  }

  return responseBody
}

function normalizeUploadResponse(response: unknown): UploadResponse {
  const dto = response as UploadResponseDto

  return {
    token: requireString(dto.token ?? dto.Token, 'token'),
    fileCount: requireNumber(dto.fileCount ?? dto.FileCount, 'fileCount'),
    expiresAt: requireString(dto.expiresAt ?? dto.ExpiresAt, 'expiresAt'),
    downloadUrl: requireString(dto.downloadUrl ?? dto.DownloadUrl, 'downloadUrl'),
  }
}

function normalizeFileInfoResponse(response: unknown): FileInfoResponse {
  const dto = response as FileInfoResponseDto

  return {
    ...normalizeUploadResponse(response),
    fileNames: requireStringArray(dto.fileNames ?? dto.FileNames, 'fileNames'),
  }
}

function createApiError(status: number, response: unknown): ApiClientError {
  const envelope = response as ErrorEnvelope
  const apiCode = envelope.error?.code
  const apiMessage = envelope.error?.message

  return new ApiClientError({
    kind: getErrorKind(status),
    status,
    code: apiCode,
    message: apiMessage ?? getFallbackErrorMessage(status),
  })
}

function createNetworkError(message = 'Network error. Check your connection.'): ApiClientError {
  return new ApiClientError({
    kind: 'network',
    code: 'network_error',
    message,
  })
}

function getErrorKind(status: number): ApiClientErrorKind {
  if (status === 400) {
    return 'bad-request'
  }

  if (status === 413) {
    return 'payload-too-large'
  }

  if (status === 404) {
    return 'not-found'
  }

  if (status === 410) {
    return 'gone'
  }

  if (status >= 500) {
    return 'server'
  }

  return 'unknown'
}

function getFallbackErrorMessage(status: number): string {
  if (status === 400) {
    return 'The request is invalid.'
  }

  if (status === 413) {
    return 'Total upload size exceeds the configured limit.'
  }

  if (status === 404) {
    return 'The requested token was not found.'
  }

  if (status === 410) {
    return 'The requested token has expired.'
  }

  if (status >= 500) {
    return 'Server error. Try again later.'
  }

  return 'Unexpected API error.'
}

function parseJson(value: string): unknown {
  if (!value) {
    return null
  }

  try {
    return JSON.parse(value)
  } catch {
    return null
  }
}

function requireString(value: unknown, fieldName: string): string {
  if (typeof value !== 'string') {
    throw invalidResponseError(fieldName)
  }

  return value
}

function requireNumber(value: unknown, fieldName: string): number {
  if (typeof value !== 'number') {
    throw invalidResponseError(fieldName)
  }

  return value
}

function requireStringArray(value: unknown, fieldName: string): string[] {
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'string')) {
    throw invalidResponseError(fieldName)
  }

  return value
}

function invalidResponseError(fieldName: string): ApiClientError {
  return new ApiClientError({
    kind: 'unknown',
    code: 'invalid_response',
    message: `API response has invalid ${fieldName}.`,
  })
}
