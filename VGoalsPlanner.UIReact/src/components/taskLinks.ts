import { TaskLinkKind, type TaskLink } from '../api/types'

export const MAX_TASK_LINKS = 30

export function linkIcon(kind: TaskLinkKind): string {
  return kind === TaskLinkKind.Url ? '🔗' : '📁'
}

// Что показываем в списке: короткое имя, а если его нет — сам адрес/путь.
export function linkLabel(link: TaskLink): string {
  const alias = link.alias?.trim()
  return alias ? alias : link.target
}

export function isHttpUrl(text: string): boolean {
  return /^https?:\/\//i.test(text.trim())
}

// Веб-адрес без схемы ("example.com/page") получает https://; пути и адреса с любой схемой остаются как есть.
export function normalizeTarget(kind: TaskLinkKind, raw: string): string {
  const text = raw.trim()
  if (kind !== TaskLinkKind.Url || text === '') {
    return text
  }
  const hasScheme = /^[a-z][a-z0-9+.-]*:\/\//i.test(text) || /^(javascript|data|vbscript|file|mailto):/i.test(text)
  return hasScheme ? text : `https://${text}`
}

// Текст ошибки или null, если значение годится.
export function validateTarget(kind: TaskLinkKind, target: string): string | null {
  if (target.trim() === '') {
    return 'Введите ссылку или путь'
  }
  if (target.length > 1000) {
    return 'Слишком длинное значение (не более 1000 символов)'
  }
  if (kind === TaskLinkKind.Url && !isHttpUrl(target)) {
    return 'Разрешены веб-адреса http:// и https://'
  }
  return null
}

export function sameLink(link: TaskLink, kind: TaskLinkKind, target: string): boolean {
  return link.kind === kind && link.target.toLowerCase() === target.toLowerCase()
}

// Копирование в буфер обмена; запасной вариант — для страниц без доступа к Clipboard API.
export async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    return true
  } catch {
    const area = document.createElement('textarea')
    area.value = text
    area.style.position = 'fixed'
    area.style.opacity = '0'
    document.body.appendChild(area)
    area.select()
    try {
      return document.execCommand('copy')
    } catch {
      return false
    } finally {
      document.body.removeChild(area)
    }
  }
}
