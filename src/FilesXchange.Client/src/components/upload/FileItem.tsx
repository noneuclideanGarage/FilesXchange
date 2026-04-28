import { formatBytes } from '../../utils/formatBytes'

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
