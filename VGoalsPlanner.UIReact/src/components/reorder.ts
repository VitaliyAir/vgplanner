// Возвращает новый порядок полного списка `all`, в котором элемент `id` стоит на месте элемента,
// который сейчас показан на позиции `visibleTargetIndex` в списке `visible` (видимое подмножество all).
// null — перемещать нечего (элемент/позиция не найдены или это то же место).
export function reorderToVisibleIndex<T>(
  all: T[],
  visible: T[],
  getId: (item: T) => string,
  id: string,
  visibleTargetIndex: number,
): T[] | null {
  const target = visible[visibleTargetIndex]
  if (target === undefined) {
    return null
  }
  const from = all.findIndex((item) => getId(item) === id)
  const to = all.findIndex((item) => getId(item) === getId(target))
  if (from === -1 || to === -1 || from === to) {
    return null
  }
  const next = [...all]
  const [moved] = next.splice(from, 1)
  next.splice(to, 0, moved)
  return next
}
