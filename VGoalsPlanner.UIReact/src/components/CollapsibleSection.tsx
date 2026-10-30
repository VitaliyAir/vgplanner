import { useState, type ReactNode } from 'react'

interface CollapsibleSectionProps {
  title: string
  className?: string
  children: ReactNode
}

export function CollapsibleSection({ title, className = '', children }: CollapsibleSectionProps) {
  const [open, setOpen] = useState(false)

  return (
    <section className={`fold-section ${open ? '' : 'fold-section--closed'} ${className}`.trim()}>
      <h2>
        <button type="button" className="fold-toggle" aria-expanded={open} onClick={() => setOpen((v) => !v)}>
          <span className="fold-chevron" aria-hidden="true">▸</span>
          {title}
        </button>
      </h2>
      {open && children}
    </section>
  )
}
