import { useMemo, useState } from 'react'
import { DropZone } from '../components/upload/DropZone'
import { FileList } from '../components/upload/FileList'
import { ProgressBar } from '../components/upload/ProgressBar'
import { UploadButton } from '../components/upload/UploadButton'
import { useUpload } from '../hooks/useUpload'

const MAX_UPLOAD_SIZE_BYTES = 2 * 1024 * 1024 * 1024

export function UploadPage() {
  const [files, setFiles] = useState<File[]>([])
  const [isDragging, setIsDragging] = useState(false)
  const { error, progress, startUpload, status } = useUpload()

  const totalSize = useMemo(
    () => files.reduce((sum, file) => sum + file.size, 0),
    [files],
  )
  const isOverLimit = totalSize > MAX_UPLOAD_SIZE_BYTES
  const isUploading = status === 'uploading'
  const uploadError = status === 'error' ? error : null
  const canUpload = files.length > 0 && !isOverLimit && !isUploading

  function handleFilesSelected(nextFiles: File[]) {
    if (isUploading || nextFiles.length === 0) {
      return
    }

    setFiles((currentFiles) => [...currentFiles, ...nextFiles])
  }

  function handleRemoveFile(indexToRemove: number) {
    if (isUploading) {
      return
    }

    setFiles((currentFiles) =>
      currentFiles.filter((_, index) => index !== indexToRemove),
    )
  }

  function handleUpload() {
    if (!canUpload) {
      return
    }

    void startUpload(files)
  }

  return (
    <main className="page-shell page-shell--upload">
      <section className="intro-section upload-panel" aria-labelledby="upload-title">
        <div className="upload-panel__header">
          <p className="eyebrow">FilesXchange</p>
          <h1 id="upload-title">Share files by temporary link</h1>
          <p className="lead">
            Select files, upload them, and send one download link.
          </p>
        </div>

        <DropZone
          disabled={isUploading}
          isDragging={isDragging}
          onDragStateChange={setIsDragging}
          onFilesSelected={handleFilesSelected}
        />

        <div className="upload-summary" aria-live="polite">
          <span>{files.length} selected</span>
          <span>{formatBytes(totalSize)} of 2 GB</span>
        </div>

        {isOverLimit ? (
          <p className="inline-error" role="alert">
            Total upload size must not exceed 2 GB. Remove one or more files.
          </p>
        ) : null}

        {uploadError ? (
          <p className="inline-error" role="alert">
            {uploadError.message}
          </p>
        ) : null}

        <FileList
          disabled={isUploading}
          files={files}
          onRemoveFile={handleRemoveFile}
        />

        {isUploading ? <ProgressBar progress={progress} /> : null}

        <UploadButton
          disabled={!canUpload}
          isUploading={isUploading}
          onUpload={handleUpload}
        />
      </section>
    </main>
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
