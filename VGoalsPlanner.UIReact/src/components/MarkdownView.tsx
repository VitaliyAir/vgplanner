import ReactMarkdown from 'react-markdown'
import rehypeRaw from 'rehype-raw'
import rehypeSanitize, { defaultSchema } from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import './markdown.css'

// Markdown не умеет "мелкий текст" и подсветку, поэтому помимо обычной разметки
// пропускаем <small> и <mark> (только с четырьмя фиксированными классами цвета);
// всё остальное сырое HTML вычищается.
const SANITIZE_SCHEMA = {
  ...defaultSchema,
  tagNames: [...(defaultSchema.tagNames ?? []), 'small', 'mark'],
  attributes: {
    ...defaultSchema.attributes,
    mark: [['className', 'ly', 'lr', 'lb', 'lg']],
  },
}

interface MarkdownViewProps {
  source: string | null | undefined
  className?: string
}

export function MarkdownView({ source, className }: MarkdownViewProps) {
  if (!source || source.trim() === '') {
    return null
  }

  return (
    <div className={className ? `md-view ${className}` : 'md-view'}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeRaw, [rehypeSanitize, SANITIZE_SCHEMA]]}
        components={{
          a: (props) => (
            <a href={props.href} target="_blank" rel="noopener noreferrer">
              {props.children}
            </a>
          ),
        }}
      >
        {source}
      </ReactMarkdown>
    </div>
  )
}
