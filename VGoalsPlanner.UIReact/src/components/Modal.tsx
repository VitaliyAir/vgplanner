import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import { createPortal } from 'react-dom'
import './modal.css'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
  className?: string
  persistent?: boolean
}

// Открытые окна; последнее — верхнее: Esc закрывает только его.
const openModals: symbol[] = []

export function Modal({ title, onClose, children, className, persistent = false }: ModalProps) {
  const onCloseRef = useRef(onClose)
  onCloseRef.current = onClose

  useEffect(() => {
    const id = Symbol('modal')
    openModals.push(id)

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !event.defaultPrevented && openModals[openModals.length - 1] === id) {
        onCloseRef.current()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      openModals.splice(openModals.indexOf(id), 1)
    }
  }, [])

  return createPortal(
    <div className="modal-backdrop" onClick={persistent ? undefined : onClose}>
      <div
        className={className ? `modal-panel ${className}` : 'modal-panel'}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="modal-header">
          <h2>{title}</h2>
          <button type="button" className="icon-button" onClick={onClose} aria-label="Закрыть">
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>,
    document.body,
  )
}
