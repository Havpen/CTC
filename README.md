# Курсы валют НБРБ

Веб-приложение показывает официальные курсы Национального банка Республики Беларусь за выбранный месяц и пересчитывает сумму из одной валюты в другую по курсу на сегодня.

Код приложения лежит в ветке `dev`.

## Что нужно установить

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) вместе с npm

Проверка:

```
dotnet --version
node --version
npm --version
```

## Как получить код

```
git clone https://github.com/Havpen/CTC.git
cd CTC
git checkout dev
```

## Как запустить

Нужны два терминала из корня проекта.

Сервер API:

```
dotnet run --project backend/CurrencyRates.Api.csproj --urls http://localhost:5080
```

Страница, во втором терминале:

```
cd frontend
npm install
npm run dev
```

Откройте http://localhost:5173

Страница ходит в API через прокси Vite, поэтому сервер должен быть запущен на порту 5080. Данные курсов берутся с https://api.nbrb.by/exrates/
