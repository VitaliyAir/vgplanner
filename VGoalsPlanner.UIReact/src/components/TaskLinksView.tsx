import { TaskLinkKind, type TaskLink } from '../api/types'
import { CopyLinkButton } from './CopyLinkButton'
import { isHttpUrl, linkIcon, linkLabel } from './taskLinks'
import './taskLinks.css'

// Список ссылок и путей задачи (только чтение): веб-адрес — ссылка, путь — текст; у каждой записи есть копирование.
export function TaskLinksView({ links }: { links: TaskLink[] }) {
  if (links.length === 0) {
    return null
  }

  return (
    <section className="task-links">
      <h4 className="task-links-title">Ссылки и пути</h4>
      <ul className="task-links-list">
        {links.map((link, index) => (
          <li key={index} className="task-links-item">
            <span aria-hidden="true">{linkIcon(link.kind)}</span>
            {link.kind === TaskLinkKind.Url && isHttpUrl(link.target) ? (
              <a href={link.target} target="_blank" rel="noopener noreferrer" className="task-links-text" title={link.target}>
                {linkLabel(link)}
              </a>
            ) : (
              <span className="task-links-text" title={link.target}>
                {linkLabel(link)}
              </span>
            )}
            <CopyLinkButton text={link.target} />
          </li>
        ))}
      </ul>
    </section>
  )
}
