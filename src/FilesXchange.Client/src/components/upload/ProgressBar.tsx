import type { UploadProgress } from '../../api/client'

type ProgressBarProps = {
  progress: UploadProgress | null
}

export function ProgressBar({ progress }: ProgressBarProps) {
  const percent = progress?.percent ?? null

  return (
    <div className="progress" aria-label="Upload progress">
      <div className="progress__meta">
        <span>Uploading</span>
        <span>{percent === null ? 'Processing...' : `${percent}%`}</span>
      </div>
      <div
        className={`progress__track${percent === null ? ' progress__track--indeterminate' : ''}`}
      >
        {percent === null ? (
          <span className="progress__indicator progress__indicator--indeterminate" />
        ) : (
          <span
            className="progress__indicator"
            style={{ width: `${percent}%` }}
          />
        )}
      </div>
    </div>
  )
}
