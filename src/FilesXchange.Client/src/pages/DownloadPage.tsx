import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiClientError, getInfo, type FileInfoResponse } from '../api/client'
import { DownloadButton } from '../components/download/DownloadButton'
import { ErrorState } from '../components/download/ErrorState'
import { TokenInfo } from '../components/download/TokenInfo'

type DownloadState =
  | { status: 'loading' }
  | { status: 'success'; info: FileInfoResponse }
  | { status: 'error'; error: ApiClientError }

export function DownloadPage() {
  const { token } = useParams<{ token: string }>()
  const [state, setState] = useState<DownloadState>({ status: 'loading' })

  useEffect(() => {
    let isCurrent = true

    async function loadInfo() {
      if (!token) {
        setState({
          status: 'error',
          error: new ApiClientError({
            kind: 'bad-request',
            status: 400,
            code: 'token_missing',
            message: 'Download token is missing.',
          }),
        })
        return
      }

      setState({ status: 'loading' })

      try {
        const info = await getInfo(token)
        if (isCurrent) {
          setState({ status: 'success', info })
        }
      } catch (error) {
        if (isCurrent) {
          setState({
            status: 'error',
            error: normalizeDownloadError(error),
          })
        }
      }
    }

    void loadInfo()

    return () => {
      isCurrent = false
    }
  }, [token])

  return (
    <main className="page-shell">
      <section className="intro-section download-panel" aria-labelledby="download-title">
        <div className="download-panel__header">
          <p className="eyebrow">Download</p>
          <h1 id="download-title">Download shared files</h1>
          <p className="lead">
            Review the shared files before starting the download.
          </p>
        </div>

        {state.status === 'loading' ? <LoadingState /> : null}

        {state.status === 'error' ? <ErrorState error={state.error} /> : null}

        {state.status === 'success' ? (
          <>
            <TokenInfo info={state.info} />
            <div className="download-actions">
              <DownloadButton downloadUrl={state.info.downloadUrl} />
              <Link className="secondary-button" to="/">
                Share files
              </Link>
            </div>
          </>
        ) : null}
      </section>
    </main>
  )
}

function LoadingState() {
  return (
    <div className="state-panel" role="status">
      <p className="eyebrow">Loading</p>
      <h2>Checking this link</h2>
      <p className="state-panel__text">Fetching file details from the server.</p>
    </div>
  )
}

function normalizeDownloadError(error: unknown): ApiClientError {
  if (error instanceof ApiClientError) {
    return error
  }

  return new ApiClientError({
    kind: 'unknown',
    code: 'unexpected_error',
    message: 'Unable to load this download link.',
  })
}
