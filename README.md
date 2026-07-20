# YandexPracticum

Проект для управления мероприятими (создание, изменение, удаление) и их бронирования.

## Технологии используемые в проекте

- .NET 10
- ASP.NET Core Web API - HTTP-хост сервиса.
- Entity Framework Core 10 - ORM для работы с базой данных.
- PostgreSQL - БД, основное хранилище данных.
- Confluent.Kafka - Асинхронный обмен событиями между сервисами (Event-Driven Architecture).

Текущий сервис бронирований, разбит на три отдельных проекта по принципам чистой архитектуры: Domain, Application, Infrastructure, Presentation. Каждый проект — отдельная сборка с чётко очерченной ответственностью, а зависимости между ними направлены только «внутрь».

## Cхема направления зависимостей в проектах

![схема направления зависимостей проектов](https://pictures.s3.yandex.net/resources/image_1779201047.png)

## Реализованные проекты

- `EventFlow.Users` — регистрация, логин и JWT-аутентификация пользователей
- `EventFlow.Events` — управление событиями
- `EventFlow.Booking` — создание, обработка и отмена бронирований

## Ролевая модель и разграничение прав

Сервис бронирования использует **JWT-аутентификацию **
JWT-токен выдаёт сервис `EventFlow.Users`, сервисы `EventFlow.Events` и `EventFlow.Booking` проверяют этот же токен, содержит GUID пользователя и роль `role`.

### Роли пользователей

| Роль    | Описание             | Права доступа                                                                                       |
| ------- | -------------------- | --------------------------------------------------------------------------------------------------- |
| `User`  | Обычный пользователь | • Обычный пользователь может просматривать события, бронировать и отменять собственные брони.       |
| `Admin` | Администратор        | • Администратор управляет событиями — создаёт, редактирует, удаляет их и может отменять любые брони |

Управление событиями доступно только роли Admin (403 для остальных), эндпоинты броней требуют аутентификации (401 без токена).

### Асинхронный обмен событиями между сервисами (Event-Driven Architecture)

Используется брокер сообщений `Kafka`.
Контракт Kafka лежит в `EventFlow.Shared/EventFlow.Entities/Brokers/BookingConfirmed.cs`.

Имя Kafka-топика, общее для издателя и подписчика лежит в `EventFlow.Shared/EventFlow.Entities/Brokers/TopicNames.cs`.

Используемый типы сообщения:

- `BookingConfirmed`

Сценарий работы:

1. Пользователь создаёт бронь со статусом `Pending`
2. Внетренний сервис `BookingBackgroundService` обрабатывает брони со статусом `Pending` и переводит их в статус `Confirmed`
3. Сервис Bookings публикует событие BookingConfirmed в Kafka при подтверждении брони.
4. Сервис Events подписан на топик и при получении события уменьшает доступные места.
5. `Events` хранит обработанные сообщения в `ProcessedMessages` для исключения повторной обработки (идемпотентности).

## Запуск проекта

### Требования

В системе должен быть установлен пакет docker и docker-compose (для ОС Windows должен быть установлен Docker Desktop) для запуска контейнера с БД PostgreSQL для хранения данных.
В системе должен быть установлен пакет .NET 10 (для ОС Linux dotnet-sdk-10.0).

### Запуск через Docker Compose

Проект запускается с использованием файла `docker-compose.yml`.
Для каждого микросервиса используется отдельный `dockerfile`:

- `EventFlow.Users/dockerfile`
- `EventFlow.Events/dockerfile`
- `EventFlow.Booking/dockerfile`

контейнеры запускаемые с использованием docker:

- `kafka`
- `akhq`
- `eventflow-user-postgres`
- `eventflow-events-postgres`
- `eventflow-booking-postgres`
- `users_api`
- `event_api`
- `booking_api`
- `redis`
- `redisinsight`

настройки для `docker-compose.yml` файла хранятся в`.env` файле в корне проекта.

Сборка и запуск контейнеров выполняется командой в Linux:

```bash
docker compose up -d
```

после запуска будут доступны слудующии web-интерфейсы:

- `Users API` — `http://localhost:5015`
- `Events API` — `http://localhost:5025`
- `Booking API` — `http://localhost:5035`
- `AKHQ` — `http://localhost:8080`

## Добавление миграции БД

### Создание миграции

Миграции создаются отдельно для каждого проекта. Для создания миграции необходимо выпонить команду

```bash
dotnet ef migrations add <MigrationName> --project <Infrastructure Project> --startup-project <Startup Project>
```

- `MigrationName` - имя миграции
- `Infrastructure Project` - проект с БД и конфигурацией
- `Startup Project` - проект со строкой подключения к БД

### Применение миграции к БД

```bash
dotnet ef database update --project <Infrastructure Project> --startup-project <Startup Project>
```

- `Infrastructure Project` - проект с БД и конфигурацией
- `Startup Project` - проект со строкой подключения к БД

### Инструкция по получению JWT-токена через Swagger

1. **Регистрация пользователя**

   Откройте Swagger UI: [https://localhost:5015/swagger/index.html](https://localhost:5015/swagger/index.html)

   Endpoint `POST /auth/register`:

   ```json
   {
     "login": "user",
     "password": "testpassword!!!",
     "role": 0
   }
   ```

   **Доступные роли:** 0 -`"User"` или 1 -`"Admin"`

   **Успешный ответ:** `204 No Content`

2. **Получение JWT-токена**

   Endpoint `POST /auth/login`:

   ```json
   {
     "login": "user",
     "password": "testpassword!!!"
   }
   ```

   **Успешный ответ:**

   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjYjc0Mjk2YS05OTYzLTQwMzYtODg4Ny1iM2I4YWYyNWJlMGUiLCJyb2xlIjoiVXNlciIsImp0aSI6IjQwMjkzZDc3LWNiNDctNDc3Yi04NDFmLTA4MTE5OTNmOWQzNyIsIm5iZiI6MTc4MjkxMzY0NiwiZXhwIjoxNzgyOTE3MjQ2LCJpYXQiOjE3ODI5MTM2NDYsImlzcyI6IkV2ZW50c0FwaUlzc3VlciIsImF1ZCI6IkV2ZW50c0FwaUF1ZGllbmNlIn0.tXoyTePqpt5EgYvaMu3z0TBtY5bELjq4KGA-3zFcvGo"
   }
   ```

3. **Авторизация в Swagger**
   - Нажмите кнопку **Authorize** в правом верхнем углу Swagger UI
   - Полученное значение `token` необходимо из метода `POST /auth/login` необходимо вставить в поле поле **Value**
   - Нажмите **Authorize**, затем **Close**

### Структура JWT-токена

Токен содержит следующие claims:

| Claim          | Значение           | Описание                                                               |
| -------------- | ------------------ | ---------------------------------------------------------------------- |
| `sub` (`Name`) | Логин пользователя | Используется для идентификации пользователя при бронировании           |
| `role`         | `User` или `Admin` | Для проверки роли в методах контроллера `[Authorize(Roles = "Admin")]` |
| `jti`          | GUID               | Уникальный идентификатор токена                                        |
| `iat`          | Unix timestamp     | Время выдачи токена                                                    |

### Настройка секрета JWT в конфигурации

#### Конфигурация для разработки (`appsettings.Development.json`)

```json
{
  "JwtTokenSettings": {
    "SchemeName": "EventsApiScheme",
    "Secret": "!234567890Qwertyuiop[Asdfghjkl;'",
    "Issuer": "EventsApiIssuer",
    "Audience": "EventsApiAudience",
    "Lifetime": 60
  }
}
```

| Параметр     | Тип    | Описание                                                                 |
| ------------ | ------ | ------------------------------------------------------------------------ |
| `SchemeName` | string | Название схемы аутентификации                                            |
| `Secret`     | string | **Секретный ключ для подписи JWT** (минимум 32 символа для HMAC SHA-256) |
| `Issuer`     | string | Издатель токена (проверяется при валидации)                              |
| `Audience`   | string | Целевая аудитория токена (проверяется при валидации)                     |
| `Lifetime`   | int    | Время жизни токена в **минутах**                                         |

### Стратегия кэширования

Для снижения нагрузки на основную базу данных и ускорения ответов API сервис `EventFlow.Events` применяет Redis в качестве кэша для часто запрашиваемых данных.

### Что кэшируется

Кэшируются два типа данных: отдельное событие (ключ `event:{guid}`, TTL 5 минут) и топ-10 событий (ключ `events:top10`, TTL 10 минут). Все ключи перечислены в классе `EventFlow.Entities.Redis`.

### Что не кэшируется

В кэш не попадают пагинированные поисковые запросы с фильтрами (GET /Events) из-за множества уникальных комбинаций параметров и сложности инвалидации при обновлении событий. Операции записи (POST, PUT, DELETE) также не кэшируются — они напрямую обращаются к БД и сбрасывают затронутые кэш-ключи.

### Стратегия инвалидации

Используется стратегия cache-aside с инвалидацией при изменении - при изменении события соответствующий ключ удаляется из кеша. Следующий читающий запрос обратится к базе и прогреет кеш заново.

| Операция                   | Инвалидируемые ключи       |
| -------------------------- | -------------------------- |
| `ChangeEventAsync`         | `event:{id}`               |
| `RemoveEventAsync`         | `event:{id}`               |
| `ReleaseSeatAsync`         | `event:{id}, events:top10` |
| `BookingConfirmedConsumer` | `event:{id}, events:top10` |

Ключ `events:top10` сбрасывается только при изменении занятости мест. При удалении события он не инвалидируется и обновляется по TTL.

### Отказоустойчивость

Для обеспечения отказоустойчивости работа с Redis абстрагирована через интерфейс `ICacheService`. При недоступности Redis:

- `GetStringAsync` возвращает null → запрос прозрачно выполнятся из БД;
- `SetStringAsync` и `KeyDeleteAsync` завершаются бесшумно (ошибки логируются, но не пробрасываются клиенту).

Такой подход делает Redis опциональным компонентом, повышающим производительность, но не влияющим на функциональность системы при сбоях.


### Конфигурация Redis

Конфигурация Redis для сервиса `EventFlow.Events` определяется в секции RedisOptions файла `appsettings.json`:

```json
{
  "RedisOptions": {
    "SingleExpirationTTL": 5,
    "TopExpirationTTL": 10
  }
}
```

Строка подключения к Redis указывается в секции `ConnectionStrings` файла `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## Методы для Cобытий

<details>
<summary>Создание нового события</summary>

- **Метод:** `POST`
- **URL:** `/Events/`
- **Параметры запроса:**
  - `title` (обязательно) - название события
  - `description` (необязательное) - описание события
  - `startAt`(обязательно) - дата начала события
  - `endAt`(обязательно) - дата окончания события
- **Пример запроса:**

```bash
curl -X 'POST' \
  'http://localhost:5025/Events' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-20T10:06:10.984Z",
  "endAt": "2026-04-21T10:06:10.984Z"
}'
```

**Пример ответа**

```json
{
  "id": "f0c88664-8563-4c24-9259-d433038f860e",
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-20T10:06:10.984Z",
  "endAt": "2026-04-21T10:06:10.984Z"
}
```

</details>

<details>
<summary>Обновления даных события</summary>

- **Метод:** `PUT`
- **URL:** `/Events/{eventId}`
- **Параметры запроса:**
  - `eventId` (обязательное) - идентификатор события
  - `title` (необязательное) - название события
  - `description` (необязательное) - описание события
  - `startAt`(необязательное) - дата начала события
  - `endAt`(необязательное) - дата окончания события

**Пример запроса**

```bash
curl -X 'PUT' \
  'http://localhost:5025/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Событие2",
  "description": "",
  "startAt": "2026-04-26T10:45:53.602Z",
  "endAt": "2026-04-28T10:45:53.602Z"
}'
```

</details>

<details>
<summary>Получение события по идетфикатору</summary>

- **Метод:** `GET`
- **URL:** `/Events/{eventId}`
- **Параметры запроса:**
  - `eventId` (обязательное) - идентификатор события

**Пример запроса**

```bash
curl -X 'GET' \
  'http://localhost:5025/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
  -H 'accept: application/json'
```

**Пример ответа**

```json
{
  "id": "43623902-6ef0-4e54-9e0d-0973e780bede",
  "title": "Событие2",
  "description": "",
  "startAt": "2026-04-26T10:45:53.602Z",
  "endAt": "2026-04-28T10:45:53.602Z"
}
```

</details>

<details>
<summary>Удаление события</summary>

- **Метод:** `DELETE`
- **URL:** `/Events/{eventId}`
- **Параметры запроса:**
  - `eventId` (обязательное) - идентификатор события

**Пример запроса**

```bash
curl -X 'DELETE' \
  'http://localhost:5025/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
  -H 'accept: */*'
```

</details>

<details>
<summary>Получение списка событий с пагинацией</summary>

- **Метод:** `GET`
- **URL:** `/Events`
- **Параметры запроса:**
  - `title` (необязательное) - название события
  - `from`(необязательное) - дата начала события
  - `to`(необязательное) - дата окончания события
  - `page`(необязательное) - cтраница, которую необходимо вернуть (по умолчанию = 1)
  - `pageSize`(необязательное) - rоличество элементов на странице (по умолчанию = 10)

**Пример запроса**

```bash
curl -X 'DELETE' \
  'http://localhost:5025/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
  -H 'accept: */*'
```

**Пример ответа**

```json
{
  "events": [
    {
      "id": "89b9e39f-fde3-471b-8279-895b5869a9df",
      "title": "Событие 1",
      "description": "",
      "startAt": "2026-04-20T10:45:26.104Z",
      "endAt": "2026-04-21T10:45:26.104Z"
    },
    {
      "id": "28a85929-3b36-4576-82ac-44c10a62ee64",
      "title": "Событие 2",
      "description": "",
      "startAt": "2026-04-20T10:45:26.104Z",
      "endAt": "2026-04-22T10:45:26.104Z"
    },
    {
      "id": "0206547a-6a36-4373-ad43-f2f67805fd94",
      "title": "Событие 3",
      "description": "",
      "startAt": "2026-04-20T10:45:26.104Z",
      "endAt": "2026-04-24T10:45:26.104Z"
    }
  ],
  "page": 1,
  "pageSize": 3,
  "totalItems": 4
}
```

</details>

### Формат ошибок ProblemDetails (RFC 7807) для событий

**Примеры ответа ошибка валидации входных параметров**

```json
{
  "type": "Validation Failed",
  "status": 400,
  "detail": "Дата начала мероприятия больше даты завершения."
}
```

**Примеры ответа ошибка неправильный идентификатор**

```json
{
  "type": "Invalid Identifier",
  "status": 404,
  "detail": "Идентификатор мероприятия не найден."
}
```

## Методы для бронирования

<details>
<summary>Создание нового бронирования</summary>

- **Метод:** `POST`
- **URL:** `/Events/{eventId}/book`
- **Параметры запроса:**
  - `eventId` (обязательно) - идентификатор события
- **Пример запроса:**

```bash
curl -X 'POST'
  'https://localhost:5035/Bookings/07c5ba9a-900a-43ba-931f-ee5f9ec58d79/book' \
  -H 'accept: */*' \
  -d ''
```

- **Пример ответа**
  - `id` - идентификатор бронирования
  - `eventID` - идентификатор события
  - `status` - статус брнонирования

```json
{
  "id": "d1a37063-0700-4c60-ae98-095be738c682",
  "eventID": "07c5ba9a-900a-43ba-931f-ee5f9ec58d79",
  "status": "Pending"
}
```

</details>

<details>
<summary>Получение информации по бронированию</summary>

- **Метод:** `GET`
- **URL:** `/Bookings/{bookingId}`
- **Параметры запроса:**
  - `bookingId` (обязательно) - идентификатор бронирования
- **Пример запроса:**

```bash
curl -X 'GET' \
  'https://localhost:5091/Bookings/d1a37063-0700-4c60-ae98-095be738c682' \
  -H 'accept: */*'
```

- **Пример ответа**
  - `id` - идентификатор бронирования
  - `eventID` - идентификатор события
  - `status` - статус брнонирования

```json
{
  "id": "d1a37063-0700-4c60-ae98-095be738c682",
  "eventID": "f1981d77-e952-420f-b9a1-13a8e2efcf8f",
  "status": "Confirmed"
}
```

</details>

<details>
<summary>Удаление (отмена) бронирования</summary>

- **Метод:** `DELETE`
- **URL:** `/Bookings/{bookingId}`
- **Параметры запроса:**
  - `bookingId` (обязательно) - идентификатор бронирования
- **Пример запроса:**

```bash
curl -X 'DELETE' \
  'http://localhost:5035/Bookings/1f8eba62-75ab-4d7a-94de-00d8dc5de092' \
  -H 'Authorization: Bearer eyJhbG...... \
  -H 'accept: */*'
```

**Успешный ответ:** `204 No Content`

</details>
