import type { FileInfoResponse } from '../../api/client'

type TokenInfoProps = {
  info: FileInfoResponse
}

export function TokenInfo({ info }: TokenInfoProps) {
  return (
    <div className="download-info">
      <div className="download-info__summary">
        <div>
          <span className="download-info__label">Token</span>
          <code>{info.token}</code>
        </div>
        <div>
          <span className="download-info__label">Files</span>
          <strong>{info.fileCount}</strong>
        </div>
        <div>
          <span className="download-info__label">Expires</span>
          <strong>{formatDate(info.expiresAt)}</strong>
        </div>
      </div>

      <div className="download-info__files">
        <span className="download-info__label">Included files</span>
        <ul className="download-file-list">
          {info.fileNames.map((fileName) => (
            <li className="download-file-list__item" key={fileName}>
              {fileName}
            </li>
          ))}
        </ul>
      </div>
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
