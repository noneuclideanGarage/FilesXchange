type UploadButtonProps = {
  disabled: boolean
  isUploading: boolean
  onUpload: () => void
}

export function UploadButton({
  disabled,
  isUploading,
  onUpload,
}: UploadButtonProps) {
  return (
    <button
      className="primary-button"
      disabled={disabled}
      onClick={onUpload}
      type="button"
    >
      {isUploading ? 'Uploading...' : 'Upload files'}
    </button>
  )
}
