import { useState } from 'react'

type CopyState = 'idle' | 'copied' | 'failed'

type CopyButtonProps = {
  value: string
}

export function CopyButton({ value }: CopyButtonProps) {
  const [copyState, setCopyState] = useState<CopyState>('idle')

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(value)
      setCopyState('copied')
    } catch {
      setCopyState('failed')
    }
  }

  return (
    <button className="copy-button" onClick={handleCopy} type="button">
      {getLabel(copyState)}
    </button>
  )
}

function getLabel(copyState: CopyState): string {
  if (copyState === 'copied') {
    return 'Copied'
  }

  if (copyState === 'failed') {
    return 'Copy failed'
  }

  return 'Copy link'
}
