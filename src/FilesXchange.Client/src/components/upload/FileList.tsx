import { FileItem } from './FileItem'

type FileListProps = {
  disabled?: boolean
  files: File[]
  onRemoveFile: (index: number) => void
}

export function FileList({
  disabled = false,
  files,
  onRemoveFile,
}: FileListProps) {
  if (files.length === 0) {
    return (
      <p className="empty-state">
        Selected files will appear here before upload.
      </p>
    )
  }

  return (
    <ul className="file-list" aria-label="Selected files">
      {files.map((file, index) => (
        <FileItem
          disabled={disabled}
          file={file}
          index={index}
          key={`${file.name}-${file.lastModified}-${index}`}
          onRemove={onRemoveFile}
        />
      ))}
    </ul>
  )
}
