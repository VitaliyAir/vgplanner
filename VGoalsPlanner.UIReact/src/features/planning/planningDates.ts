export function toIsoDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function mondayOf(d: Date): Date {
  const result = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  const day = result.getDay() // 0=Sun..6=Sat
  const diff = day === 0 ? -6 : 1 - day
  result.setDate(result.getDate() + diff)
  return result
}

export function addDays(d: Date, n: number): Date {
  const result = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  result.setDate(result.getDate() + n)
  return result
}

export function addMonths(d: Date, n: number): Date {
  return new Date(d.getFullYear(), d.getMonth() + n, 1)
}

// Пн-стартовая сетка на весь месяц: 6 недель (42 даты) от понедельника на/до
// 1-го числа, затем отбрасываем последнюю неделю, если она целиком вне
// месяца — так получается динамически 5 или 6 строк вместо жёстких 5.
export function monthGridDates(year: number, month: number): Date[] {
  const firstOfMonth = new Date(year, month - 1, 1)
  const start = mondayOf(firstOfMonth)
  const dates: Date[] = []
  for (let i = 0; i < 42; i++) {
    dates.push(addDays(start, i))
  }

  const lastWeek = dates.slice(35, 42)
  const lastWeekHasMonthDay = lastWeek.some((d) => d.getFullYear() === year && d.getMonth() === month - 1)

  return lastWeekHasMonthDay ? dates : dates.slice(0, 35)
}

function capitalizeFirst(s: string): string {
  return s.length === 0 ? s : s[0].toUpperCase() + s.slice(1)
}

export function formatDayLabel(d: Date): string {
  return capitalizeFirst(new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' }).format(d))
}

export function formatWeekLabel(weekStart: Date): string {
  const weekEnd = addDays(weekStart, 6)
  const sameMonth = weekStart.getMonth() === weekEnd.getMonth() && weekStart.getFullYear() === weekEnd.getFullYear()
  const monthYearFmt = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' })

  if (sameMonth) {
    const dayFmt = new Intl.DateTimeFormat('ru-RU', { day: 'numeric' })
    return `${dayFmt.format(weekStart)}–${monthYearFmt.format(weekEnd)}`
  }

  const startFmt = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long' })
  return `${startFmt.format(weekStart)} – ${monthYearFmt.format(weekEnd)}`
}

export function formatMonthLabel(d: Date): string {
  return capitalizeFirst(new Intl.DateTimeFormat('ru-RU', { month: 'long', year: 'numeric' }).format(d))
}
