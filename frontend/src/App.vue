<script setup>
import RatesTable from './components/RatesTable.vue'
import Converter from './components/Converter.vue'
</script>

<template>
  <main class="container">
    <h1>Курсы валют НБРБ</h1>
    <p class="lead">Официальные курсы Национального банка за выбранный месяц и калькулятор на последний доступный день.</p>
    <RatesTable />
    <Converter />
  </main>
</template>
