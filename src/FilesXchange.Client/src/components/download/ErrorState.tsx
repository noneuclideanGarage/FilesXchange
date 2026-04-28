import { Link } from 'react-router-dom'
import type { ApiClientError } from '../../api/client'

type ErrorStateProps = {
  error: ApiClientError
}

export function ErrorState({ error }: ErrorStateProps) {
  const content = getErrorContent(error)

  return (
    <div className="state-panel state-panel--error" role="alert">
      <p className="eyebrow">{content.label}</p>
      <h2>{content.title}</h2>
      <p className="state-panel__text">{content.message}</p>
      <Link className="text-link" to="/">
        Back to upload
      </Link>
    </div>
  )
}

function getErrorContent(error: ApiClientError): {
  label: string
  message: string
  title: string
} {
  if (error.kind === 'not-found') {
    return {
      label: '404',
      title: 'Link not found',
      message: 'This token does not match an active file exchange.',
    }
  }

  if (error.kind === 'gone') {
    return {
      label: '410',
      title: 'Link expired',
      message: 'This file exchange is no longer available.',
    }
  }

  if (error.kind === 'server') {
    return {
      label: '500',
      title: 'Server problem',
      message: 'The server could not load this file exchange. Try again later.',
    }
  }

  if (error.kind === 'network') {
    return {
      label: 'Network',
      title: 'Connection problem',
      message: 'Check your connection and reload the page.',
    }
  }

  return {
    label: 'Error',
    title: 'Unable to load link',
    message: error.message,
  }
}
