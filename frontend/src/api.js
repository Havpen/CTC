async function getJson(url) {
  const response = await fetch(url)
  if (!response.ok) {
    let message = 'Не удалось получить данные'
    try {
      const body = await response.json()
      if (body.message)
        message = body.message
    }
    catch {
      // у ошибки нет JSON, остаётся общий текст
    }
    throw new Error(message)
  }

  return response.json()
}

export function getCurrencies() {
  return getJson('/api/currencies')
}

export function getRates(currency, year, month) {
  const params = new URLSearchParams({ currency, year, month })
  return getJson(`/api/rates?${params}`)
}

export function getSummary(currency, year, month) {
  const params = new URLSearchParams({ currency, year, month })
  return getJson(`/api/rates/summary?${params}`)
}

export function convert(from, to, amount) {
  const params = new URLSearchParams({ from, to, amount })
  return getJson(`/api/convert?${params}`)
}
