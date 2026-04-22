import { Link, useParams } from 'react-router-dom'

export function DownloadPage() {
  const { token } = useParams<{ token: string }>()

  return (
    <main className="page-shell">
      <section className="intro-section" aria-labelledby="download-title">
        <p className="eyebrow">Download</p>
        <h1 id="download-title">Download shared files</h1>
        <p className="lead">
          Token: <code>{token}</code>
        </p>
        <Link className="text-link" to="/">
          Back to upload
        </Link>
      </section>
    </main>
  )
}
