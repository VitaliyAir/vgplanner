import { useRef, useState } from 'react'
import type { MouseEvent } from 'react'
import { MarkdownView } from './MarkdownView'
import './markdown.css'

const DEFAULT_MAX_LENGTH = 20000
const LINK_URL_REGEX = /^(https?:\/\/|mailto:)\S+$/i
const MARK_CLOSE = '</mark>'
const MARK_OPEN_AT_END_REGEX = /<mark class="(?:ly|lr|lb|lg)">$/

// Цвета подсветки — фиксированный набор; тот же список разрешён в MarkdownView.
const HIGHLIGHT_COLORS = [
  { code: 'ly', label: 'Y', title: 'Светло-жёлтый' },
  { code: 'lr', label: 'R', title: 'Светло-красный' },
  { code: 'lb', label: 'B', title: 'Светло-синий' },
  { code: 'lg', label: 'G', title: 'Светло-зелёный' },
] as const

interface MarkdownEditorProps {
  value: string
  onChange: (value: string) => void
  id?: string
  placeholder?: string
  disabled?: boolean
  autoFocus?: boolean
  maxLength?: number
  compact?: boolean
}

// Убирает префикс заголовка и обёртку <small> — база для Крупный/Обычный/Мелкий.
function stripLineFormat(line: string): string {
  return line.replace(/^#{1,6}\s+/, '').replace(/<\/?small>/g, '')
}

function stripMarks(text: string): string {
  return text.replace(/<mark class="(?:ly|lr|lb|lg)">/g, '').replace(/<\/mark>/g, '')
}

export function MarkdownEditor({
  value,
  onChange,
  id,
  placeholder,
  disabled = false,
  autoFocus = false,
  maxLength = DEFAULT_MAX_LENGTH,
  compact = false,
}: MarkdownEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const [mode, setMode] = useState<'edit' | 'preview'>('edit')
  const [linkError, setLinkError] = useState<string | null>(null)

  // execCommand('insertText') сохраняет историю отмены (Ctrl+Z) в textarea;
  // если браузер его не поддерживает — обычное обновление состояния.
  const replaceRange = (start: number, end: number, text: string, selectStart: number, selectEnd: number) => {
    const el = textareaRef.current
    if (!el) {
      return
    }
    el.focus()
    el.setSelectionRange(start, end)
    const handled = document.execCommand('insertText', false, text)
    if (!handled) {
      onChange(value.slice(0, start) + text + value.slice(end))
    }
    requestAnimationFrame(() => el.setSelectionRange(selectStart, selectEnd))
  }

  const currentSelection = () => {
    const el = textareaRef.current
    return { start: el?.selectionStart ?? 0, end: el?.selectionEnd ?? 0 }
  }

  const applyToLines = (transform: (line: string) => string) => {
    const { start, end } = currentSelection()
    const lineStart = value.lastIndexOf('\n', start - 1) + 1
    const effectiveEnd = end > start && value[end - 1] === '\n' ? end - 1 : end
    const newlineAfter = value.indexOf('\n', effectiveEnd)
    const lineEnd = newlineAfter === -1 ? value.length : newlineAfter

    const block = value.slice(lineStart, lineEnd)
    const transformed = block
      .split('\n')
      .map((line) => (line.trim() === '' ? line : transform(stripLineFormat(line))))
      .join('\n')

    replaceRange(lineStart, lineEnd, transformed, lineStart, lineStart + transformed.length)
  }

  const handleBold = () => {
    const { start, end } = currentSelection()
    const selected = value.slice(start, end)

    if (selected === '') {
      replaceRange(start, end, '**текст**', start + 2, start + 7)
      return
    }
    if (value.slice(start - 2, start) === '**' && value.slice(end, end + 2) === '**') {
      replaceRange(start - 2, end + 2, selected, start - 2, start - 2 + selected.length)
      return
    }
    if (selected.length >= 4 && selected.startsWith('**') && selected.endsWith('**')) {
      const inner = selected.slice(2, -2)
      replaceRange(start, end, inner, start, start + inner.length)
      return
    }
    replaceRange(start, end, `**${selected}**`, start + 2, start + 2 + selected.length)
  }

  // Каждая непустая строка выделения оборачивается/разворачивается отдельно,
  // чтобы ~~ не пересекал границы абзацев.
  const handleStrike = () => {
    const { start, end } = currentSelection()
    const selected = value.slice(start, end)

    if (selected === '') {
      replaceRange(start, end, '~~текст~~', start + 2, start + 7)
      return
    }
    if (value.slice(start - 2, start) === '~~' && value.slice(end, end + 2) === '~~') {
      replaceRange(start - 2, end + 2, selected, start - 2, start - 2 + selected.length)
      return
    }

    const lines = selected.split('\n')
    const nonEmpty = lines.filter((line) => line.trim() !== '')
    const allWrapped = nonEmpty.length > 0 && nonEmpty.every((line) => /^\s*~~.+~~\s*$/.test(line))

    const result = lines
      .map((line) => {
        if (line.trim() === '') {
          return line
        }
        return allWrapped ? line.replace(/^(\s*)~~(.*)~~(\s*)$/, '$1$2$3') : line.replace(/^(\s*)(.*?)(\s*)$/, '$1~~$2~~$3')
      })
      .join('\n')

    replaceRange(start, end, result, start, start + result.length)
  }

  // color === null — снять подсветку. Каждая непустая строка выделения оборачивается
  // отдельно, чтобы <mark> не пересекал границы абзацев Markdown.
  const handleHighlight = (color: string | null) => {
    let { start, end } = currentSelection()

    const openMatch = value.slice(0, start).match(MARK_OPEN_AT_END_REGEX)
    if (openMatch && value.slice(end).startsWith(MARK_CLOSE)) {
      start -= openMatch[0].length
      end += MARK_CLOSE.length
    }

    const plain = stripMarks(value.slice(start, end))

    if (color === null) {
      replaceRange(start, end, plain, start, start + plain.length)
      return
    }

    if (plain === '') {
      const open = `<mark class="${color}">`
      replaceRange(start, end, `${open}текст${MARK_CLOSE}`, start + open.length, start + open.length + 5)
      return
    }

    const wrapped = plain
      .split('\n')
      .map((line) => (line.trim() === '' ? line : `<mark class="${color}">${line}${MARK_CLOSE}`))
      .join('\n')
    replaceRange(start, end, wrapped, start, start + wrapped.length)
  }

  const handleLink = () => {
    const { start, end } = currentSelection()
    const selected = value.slice(start, end)

    const input = window.prompt('Адрес ссылки (http://, https:// или mailto:)', 'https://')
    if (input === null) {
      return
    }
    const url = input.trim()
    if (!LINK_URL_REGEX.test(url)) {
      setLinkError('Ссылка должна начинаться с http://, https:// или mailto:')
      return
    }
    setLinkError(null)

    const text = selected === '' ? 'ссылка' : selected
    const safeUrl = url.replace(/\(/g, '%28').replace(/\)/g, '%29')
    replaceRange(start, end, `[${text}](${safeUrl})`, start + 1, start + 1 + text.length)
  }

  const toolbarDisabled = disabled || mode === 'preview'
  // mousedown по кнопке не должен снимать фокус/выделение с textarea.
  const keepSelection = (event: MouseEvent) => event.preventDefault()

  return (
    <div className={compact ? 'md-editor md-editor--compact' : 'md-editor'}>
      <div className="md-toolbar">
        <div className="md-toolbar-buttons">
          <button type="button" className="md-btn md-btn--bold" onMouseDown={keepSelection} onClick={handleBold} disabled={toolbarDisabled} title="Жирный">
            B
          </button>
          <button type="button" className="md-btn md-btn--strike" onMouseDown={keepSelection} onClick={handleStrike} disabled={toolbarDisabled} title="Зачёркнутый">
            S
          </button>
          <button type="button" className="md-btn" onMouseDown={keepSelection} onClick={() => applyToLines((l) => `## ${l}`)} disabled={toolbarDisabled} title="Крупный текст (строка)">
            Крупный
          </button>
          <button type="button" className="md-btn" onMouseDown={keepSelection} onClick={() => applyToLines((l) => l)} disabled={toolbarDisabled} title="Обычный размер (строка)">
            Обычный
          </button>
          <button type="button" className="md-btn md-btn--small" onMouseDown={keepSelection} onClick={() => applyToLines((l) => `<small>${l}</small>`)} disabled={toolbarDisabled} title="Мелкий текст (строка)">
            Мелкий
          </button>
          <button type="button" className="md-btn" onMouseDown={keepSelection} onClick={handleLink} disabled={toolbarDisabled} title="Внешняя ссылка">
            Ссылка
          </button>

          <span className="md-toolbar-sep" aria-hidden="true" />

          {HIGHLIGHT_COLORS.map((color) => (
            <button
              key={color.code}
              type="button"
              className={`md-btn md-color md-color--${color.code}`}
              onMouseDown={keepSelection}
              onClick={() => handleHighlight(color.code)}
              disabled={toolbarDisabled}
              title={`${color.title} (выделение цветом)`}
            >
              {color.label}
            </button>
          ))}
          <button type="button" className="md-btn" onMouseDown={keepSelection} onClick={() => handleHighlight(null)} disabled={toolbarDisabled} title="Убрать выделение цветом">
            ✕
          </button>
        </div>

        <div className="md-mode-switch">
          <button type="button" className={mode === 'edit' ? 'active' : ''} onClick={() => setMode('edit')} disabled={disabled}>
            Правка
          </button>
          <button type="button" className={mode === 'preview' ? 'active' : ''} onClick={() => setMode('preview')} disabled={disabled}>
            Просмотр
          </button>
        </div>
      </div>

      {linkError && <p className="tree-error tree-error--inline">{linkError}</p>}

      {mode === 'edit' ? (
        <textarea
          ref={textareaRef}
          id={id}
          className="md-textarea"
          value={value}
          maxLength={maxLength}
          placeholder={placeholder}
          disabled={disabled}
          autoFocus={autoFocus}
          onChange={(event) => onChange(event.target.value)}
        />
      ) : (
        <div className="md-preview">
          {value.trim() === '' ? <p className="md-preview-empty">Пока нечего показывать</p> : <MarkdownView source={value} />}
        </div>
      )}
    </div>
  )
}
