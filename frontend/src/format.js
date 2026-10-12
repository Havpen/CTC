const rateFormat = new Intl.NumberFormat('ru-RU', {
  minimumFractionDigits: 4,
  maximumFractionDigits: 4
})

const changeFormat = new Intl.NumberFormat('ru-RU', {
  minimumFractionDigits: 4,
  maximumFractionDigits: 4,
  signDisplay: 'exceptZero'
})

export function formatRate(value) {
  return rateFormat.format(value)
}

export function formatChange(value) {
  if (value == null)
    return '—'
  return changeFormat.format(value)
}

export function formatDate(value) {
  const [year, month, day] = String(value).slice(0, 10).split('-')
  if (!day)
    return String(value)
  return `${day}.${month}.${year}`
}
