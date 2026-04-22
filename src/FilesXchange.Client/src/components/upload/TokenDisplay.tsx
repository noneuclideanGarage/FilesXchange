import { CopyButton } from './CopyButton'

type TokenDisplayProps = {
  downloadUrl: string
  token: string
}

export function TokenDisplay({ downloadUrl, token }: TokenDisplayProps) {
  return (
    <div className="token-display">
      <div className="token-display__row">
        <span className="token-display__label">Token</span>
        <code className="token-display__value">{token}</code>
      </div>
      <div className="token-display__row token-display__row--link">
        <span className="token-display__label">Link</span>
        <a className="token-display__value" href={downloadUrl}>
          {downloadUrl}
        </a>
        <CopyButton value={downloadUrl} />
      </div>
    </div>
  )
}
