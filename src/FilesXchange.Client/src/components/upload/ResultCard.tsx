import type { UploadResponse } from '../../api/client'
import { TokenDisplay } from './TokenDisplay'

type ResultCardProps = {
  result: UploadResponse
  onReset: () => void
}

export function ResultCard({ result, onReset }: ResultCardProps) {
  return (
    <div className="result-card" role="status">
      <div className="result-card__header">
        <p className="eyebrow">Ready to share</p>
        <h2>Your link is ready</h2>
        <p className="result-card__meta">
          {result.fileCount} file{result.fileCount === 1 ? '' : 's'} available
          until {formatDate(result.expiresAt)}.
        </p>
      </div>

      <TokenDisplay downloadUrl={result.downloadUrl} token={result.token} />

      <button className="secondary-button" onClick={onReset} type="button">
        Share more
      </button>
    </div>
  )
}

function formatDate(value: string): string {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}
