import { apiDelete, apiDownload, apiUpload } from './httpClient'
import type { TaskAttachment } from './types'

// POST /api/vtasks/{id}/attachments — загрузить файл во вложения задачи.
export function uploadTaskAttachment(taskId: string, file: File): Promise<TaskAttachment> {
  return apiUpload<TaskAttachment>(`/vtasks/${taskId}/attachments?fileName=${encodeURIComponent(file.name)}`, file)
}

// DELETE /api/vtasks/{id}/attachments/{attachmentId}
export function deleteTaskAttachment(taskId: string, attachmentId: string): Promise<void> {
  return apiDelete(`/vtasks/${taskId}/attachments/${attachmentId}`)
}

// GET /api/vtasks/{id}/attachments/{attachmentId} — скачивает файл и сохраняет его через временную ссылку.
export async function downloadTaskAttachment(taskId: string, attachment: TaskAttachment): Promise<void> {
  const blob = await apiDownload(`/vtasks/${taskId}/attachments/${attachment.id}`)
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = attachment.fileName
  document.body.appendChild(link)
  link.click()
  link.remove()

  // Браузеру нужно время начать скачивание, прежде чем освобождать адрес.
  setTimeout(() => URL.revokeObjectURL(url), 10_000)
}
