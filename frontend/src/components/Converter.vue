<script setup>
import { onMounted, ref, watch } from 'vue'
import { convert, getCurrencies } from '../api'
import { formatDate, formatRate } from '../format'

const currencies = ref([])
const fromCode = ref('BYN')
const toCode = ref('USD')
const amount = ref('100')
const result = ref(null)
const error = ref('')
const loading = ref(false)
const ready = ref(false)

let timer = 0
let requestId = 0

function parseAmount(text) {
  const value = Number(String(text).trim().replace(',', '.'))
  if (!Number.isFinite(value) || value <= 0)
    return null
  return value
}

async function calculate() {
  const value = parseAmount(amount.value)
  if (value == null) {
    result.value = null
    error.value = 'Введите число больше нуля'
    return
  }

  const id = ++requestId
  loading.value = true
  error.value = ''

  try {
    const data = await convert(fromCode.value, toCode.value, value)
    if (id !== requestId)
      return
    result.value = data
  }
  catch (reason) {
    if (id !== requestId)
      return
    result.value = null
    error.value = reason.message
  }
  finally {
    if (id === requestId)
      loading.value = false
  }
}

function schedule() {
  if (!ready.value)
    return
  clearTimeout(timer)
  timer = setTimeout(calculate, 300)
}

function swap() {
  const previous = fromCode.value
  fromCode.value = toCode.value
  toCode.value = previous
}

onMounted(async () => {
  try {
    currencies.value = await getCurrencies()
    if (!currencies.value.some(item => item.code === 'USD'))
      toCode.value = currencies.value.find(item => item.code !== 'BYN')?.code ?? 'BYN'
    ready.value = true
    await calculate()
  }
  catch (reason) {
    error.value = reason.message
  }
})

watch([fromCode, toCode, amount], schedule)
</script>

<template>
  <article>
    <h2>Калькулятор</h2>
    <p>По последнему доступному курсу на сегодня.</p>
    <form @submit.prevent="calculate">
      <label>
        Из
        <select v-model="fromCode">
          <option v-for="item in currencies" :key="item.code" :value="item.code">
            {{ item.code }} — {{ item.name }}
          </option>
        </select>
      </label>
      <label>
        Сумма
        <input v-model="amount" type="number" min="0" step="0.01" inputmode="decimal" />
      </label>
      <button type="button" class="secondary" @click="swap">Поменять местами</button>
      <label>
        В
        <select v-model="toCode">
          <option v-for="item in currencies" :key="`to-${item.code}`" :value="item.code">
            {{ item.code }} — {{ item.name }}
          </option>
        </select>
      </label>
      <button type="submit">Рассчитать</button>
    </form>

    <p v-if="loading">Считаю…</p>
    <p v-if="error" class="error">{{ error }}</p>
    <output v-if="result">
      <strong>{{ formatRate(result.amount) }} {{ result.from }} = {{ formatRate(result.result) }} {{ result.to }}</strong>
      <span>{{ result.description }}</span>
      <span>Дата курса: {{ formatDate(result.rateDate) }}</span>
    </output>
  </article>
</template>
