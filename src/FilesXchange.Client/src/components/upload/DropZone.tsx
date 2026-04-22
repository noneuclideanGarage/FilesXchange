import type { ChangeEvent, DragEvent } from 'react'

type DropZoneProps = {
  isDragging: boolean
  onDragStateChange: (isDragging: boolean) => void
  onFilesSelected: (files: File[]) => void
}

export function DropZone({
  isDragging,
  onDragStateChange,
  onFilesSelected,
}: DropZoneProps) {
  function handleDragEnter(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    onDragStateChange(true)
  }

  function handleDragOver(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
  }

  function handleDragLeave(event: DragEvent<HTMLLabelElement>) {
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) {
      return
    }

    onDragStateChange(false)
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    onDragStateChange(false)
    onFilesSelected(Array.from(event.dataTransfer.files))
  }

  function handleInputChange(event: ChangeEvent<HTMLInputElement>) {
    onFilesSelected(Array.from(event.currentTarget.files ?? []))
    event.currentTarget.value = ''
  }

  return (
    <label
      className={`drop-zone${isDragging ? ' drop-zone--dragging' : ''}`}
      onDragEnter={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
    >
      <span className="drop-zone__title">Drop files here</span>
      <span className="drop-zone__hint">or click to select from your device</span>
      <input
        className="drop-zone__input"
        multiple
        onChange={handleInputChange}
        type="file"
      />
    </label>
  )
}
