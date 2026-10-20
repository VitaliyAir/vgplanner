import { useEffect, useRef, useState } from 'react'
import { copyText } from './taskLinks'

// Копирует адрес/путь (не короткое имя) в буфер обмена; на 1.5 с значок меняется на галочку.
export function CopyLinkButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false)
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const handleClick = async () => {
    if (await copyText(text)) {
      setCopied(true)
      window.clearTimeout(timer.current)
      timer.current = window.setTimeout(() => setCopied(false), 1500)
    }
  }

  return (
    <button
      type="button"
      className="icon-button"
      title={copied ? 'Скопировано' : 'Копировать в буфер обмена'}
      aria-label="Копировать в буфер обмена"
      onClick={handleClick}
    >
      {copied ? '✓' : '⧉'}
    </button>
  )
}
