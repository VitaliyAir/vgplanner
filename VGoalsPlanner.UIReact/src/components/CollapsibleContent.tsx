import { useLayoutEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import './collapsibleContent.css'

// Длинное содержимое показывается усечённым (maxHeight) с кнопкой "развернуть"; у короткого кнопки нет.
export function CollapsibleContent({ children, maxHeight = 80 }: { children: ReactNode; maxHeight?: number }) {
  const bodyRef = useRef<HTMLDivElement>(null)
  const [expanded, setExpanded] = useState(false)
  const [overflowing, setOverflowing] = useState(false)

  useLayoutEffect(() => {
    const body = bodyRef.current
    if (!body) {
      return
    }
    const measure = () => setOverflowing(body.scrollHeight > maxHeight + 1)
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(body)
    return () => observer.disconnect()
  }, [maxHeight, children])

  const clamped = overflowing && !expanded

  return (
    <div className="collapsible">
      <div
        ref={bodyRef}
        className={`collapsible-body${clamped ? ' collapsible-body--clamped' : ''}`}
        style={expanded ? undefined : { maxHeight }}
      >
        {children}
      </div>
      {overflowing && (
        <button
          type="button"
          className="collapsible-toggle"
          aria-expanded={expanded}
          title={expanded ? 'Свернуть' : 'Развернуть'}
          onClick={() => setExpanded((value) => !value)}
        >
          {expanded ? '▴' : '▾'}
        </button>
      )}
    </div>
  )
}
