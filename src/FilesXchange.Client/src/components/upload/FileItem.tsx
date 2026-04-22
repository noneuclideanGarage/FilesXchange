type FileItemProps = {
  disabled?: boolean
  file: File
  index: number
  onRemove: (index: number) => void
}

export function FileItem({
  disabled = false,
  file,
  index,
  onRemove,
}: FileItemProps) {
  return (
    <li className="file-item">
      <div className="file-item__meta">
        <span className="file-item__name" title={file.name}>
          {file.name}
        </span>
        <span className="file-item__size">{formatBytes(file.size)}</span>
      </div>
      <button
        aria-label={`Remove ${file.name}`}
        className="icon-button"
        disabled={disabled}
        onClick={() => onRemove(index)}
        type="button"
      >
        x
      </button>
    </li>
  )
}

function formatBytes(bytes: number): string {
  if (bytes === 0) {
    return '0 B'
  }

  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const unitIndex = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    units.length - 1,
  )
  const value = bytes / 1024 ** unitIndex

  return `${value.toFixed(value >= 10 || unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`
}
