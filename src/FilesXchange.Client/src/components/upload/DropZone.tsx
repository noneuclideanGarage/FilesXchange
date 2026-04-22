import type { ChangeEvent, DragEvent } from 'react'

type DropZoneProps = {
  disabled?: boolean
  isDragging: boolean
  onDragStateChange: (isDragging: boolean) => void
  onFilesSelected: (files: File[]) => void
}

export function DropZone({
  disabled = false,
  isDragging,
  onDragStateChange,
  onFilesSelected,
}: DropZoneProps) {
  function handleDragEnter(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    if (disabled) {
      return
    }

    onDragStateChange(true)
  }

  function handleDragOver(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
  }

  function handleDragLeave(event: DragEvent<HTMLLabelElement>) {
    if (disabled) {
      return
    }

    if (event.currentTarget.contains(event.relatedTarget as Node | null)) {
      return
    }

    onDragStateChange(false)
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    onDragStateChange(false)
    if (disabled) {
      return
    }

    onFilesSelected(Array.from(event.dataTransfer.files))
  }

  function handleInputChange(event: ChangeEvent<HTMLInputElement>) {
    if (disabled) {
      event.currentTarget.value = ''
      return
    }

    onFilesSelected(Array.from(event.currentTarget.files ?? []))
    event.currentTarget.value = ''
  }

  return (
    <label
      className={[
        'drop-zone',
        isDragging ? 'drop-zone--dragging' : '',
        disabled ? 'drop-zone--disabled' : '',
      ]
        .filter(Boolean)
        .join(' ')}
      onDragEnter={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
    >
      <span className="drop-zone__title">Drop files here</span>
      <span className="drop-zone__hint">or click to select from your device</span>
      <input
        className="drop-zone__input"
        disabled={disabled}
        multiple
        onChange={handleInputChange}
        type="file"
      />
    </label>
  )
}
