type DownloadButtonProps = {
  downloadUrl: string
}

export function DownloadButton({ downloadUrl }: DownloadButtonProps) {
  function handleDownload() {
    window.location.assign(downloadUrl)
  }

  return (
    <button className="primary-button" onClick={handleDownload} type="button">
      Download files
    </button>
  )
}
