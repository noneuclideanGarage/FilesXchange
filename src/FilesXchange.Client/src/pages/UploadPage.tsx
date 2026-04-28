import { useMemo, useState } from 'react'
import { DropZone } from '../components/upload/DropZone'
import { FileList } from '../components/upload/FileList'
import { ProgressBar } from '../components/upload/ProgressBar'
import { ResultCard } from '../components/upload/ResultCard'
import { UploadButton } from '../components/upload/UploadButton'
import { useUpload } from '../hooks/useUpload'
import { formatBytes } from '../utils/formatBytes'

const MAX_UPLOAD_SIZE_BYTES = 2 * 1024 * 1024 * 1024

export function UploadPage() {
  const [files, setFiles] = useState<File[]>([])
  const [isDragging, setIsDragging] = useState(false)
  const { error, progress, resetUpload, result, startUpload, status } =
    useUpload()

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

  function handleReset() {
    setFiles([])
    setIsDragging(false)
    resetUpload()
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

        {status === 'success' && result ? (
          <ResultCard result={result} onReset={handleReset} />
        ) : (
          <>
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
          </>
        )}
      </section>
    </main>
  )
}
