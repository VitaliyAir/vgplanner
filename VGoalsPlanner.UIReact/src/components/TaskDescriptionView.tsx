import { MarkdownView } from './MarkdownView'

interface TaskDescriptionViewProps {
  description: string | null
}

export function TaskDescriptionView({ description }: TaskDescriptionViewProps) {
  if (!description || description.trim() === '') {
    return <p>Без описания</p>
  }

  return <MarkdownView source={description} />
}
