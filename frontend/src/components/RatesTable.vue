<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { getCurrencies, getRates, getSummary } from '../api'
import { formatChange, formatDate, formatRate } from '../format'

const months = [
  { value: 1, name: 'Январь' },
  { value: 2, name: 'Февраль' },
  { value: 3, name: 'Март' },
  { value: 4, name: 'Апрель' },
  { value: 5, name: 'Май' },
  { value: 6, name: 'Июнь' },
  { value: 7, name: 'Июль' },
  { value: 8, name: 'Август' },
  { value: 9, name: 'Сентябрь' },
  { value: 10, name: 'Октябрь' },
  { value: 11, name: 'Ноябрь' },
  { value: 12, name: 'Декабрь' }
]

const today = new Date()
const years = Array.from({ length: 11 }, (_, index) => today.getFullYear() - index)

const month = ref(today.getMonth() + 1)
const year = ref(today.getFullYear())
const currencies = ref([])
const currency = ref('USD')
const rows = ref([])
const summary = ref(null)
const error = ref('')
const loading = ref(false)
const ready = ref(false)

let requestId = 0

const selected = computed(() => currencies.value.find(item => item.code === currency.value))

async function loadCurrencies() {
  currencies.value = await getCurrencies()
  if (!currencies.value.some(item => item.code === currency.value) && currencies.value.length > 0)
    currency.value = currencies.value.find(item => item.code === 'USD')?.code ?? currencies.value[0].code
}

async function loadRates() {
  const id = ++requestId
  loading.value = true
  error.value = ''

  try {
    const [rateRows, rateSummary] = await Promise.all([
      getRates(currency.value, year.value, month.value),
      getSummary(currency.value, year.value, month.value)
    ])
    if (id !== requestId)
      return

    rows.value = rateRows
    summary.value = rateSummary
  }
  catch (reason) {
    if (id !== requestId)
      return

    rows.value = []
    summary.value = null
    error.value = reason.message
  }
  finally {
    if (id === requestId)
      loading.value = false
  }
}

onMounted(async () => {
  try {
    await loadCurrencies()
    await loadRates()
    ready.value = true
  }
  catch (reason) {
    error.value = reason.message
  }
})

watch([currency, year, month], () => {
  if (ready.value)
    loadRates()
})
</script>

<template>
  <article>
    <h2>Курсы за месяц</h2>
    <form>
      <label>
        Месяц
        <select v-model.number="month">
          <option v-for="item in months" :key="item.value" :value="item.value">{{ item.name }}</option>
        </select>
      </label>
      <label>
        Год
        <select v-model.number="year">
          <option v-for="item in years" :key="item" :value="item">{{ item }}</option>
        </select>
      </label>
      <label>
        Валюта
        <select v-model="currency">
          <option v-for="item in currencies" :key="item.code" :value="item.code">
            {{ item.code }} — {{ item.name }}
          </option>
        </select>
      </label>
    </form>

    <p v-if="selected && selected.scale !== 1">
      Официальный курс указан за {{ selected.scale }} {{ selected.code }}.
    </p>
    <p v-if="loading">Загрузка…</p>
    <p v-if="error" class="error">{{ error }}</p>

    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Дата</th>
            <th>Официальный курс (BYN)</th>
            <th>Изменение к предыдущему дню (BYN)</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.date">
            <td>{{ formatDate(row.date) }}</td>
            <td>{{ formatRate(row.officialRate) }}</td>
            <td :class="{ up: row.change > 0, down: row.change < 0 }">{{ formatChange(row.change) }}</td>
          </tr>
        </tbody>
        <tfoot v-if="summary">
          <tr>
            <th>За месяц</th>
            <td colspan="2">
              средний {{ formatRate(summary.average) }},
              максимальный {{ formatRate(summary.max) }},
              минимальный {{ formatRate(summary.min) }}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  </article>
</template>
