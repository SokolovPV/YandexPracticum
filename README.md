# YandexPracticum

Проект API для управления мероприятими (создание, изменение, кдаление) и их бронирования.

`Проект разрабатывается в VS Code, версия NET.10 `

Текущий сервис бронирований, разбит на четыре отдельных проекта по принципам чистой архитектуры: Domain, Application, Infrastructure, Presentation. Каждый проект — отдельная сборка с чётко очерченной ответственностью, а зависимости между ними направлены только «внутрь». 


# Domain (EventsApi.Domain)
Проект описывает предметную область и не зависит от технологий.
Содержит:
- доменные сущности и перечисления -  Event (событие), Booking (бронирование), BookingStatus (перечисление статусов бронирования). Реализованы фабричные методы создания события и брони (Event.Create(...), Booking.Create(...), а так же т бизнес-правила (к примеру Event.TryReserveSeats(), Booking.Confirm(), Booking.Confirm() и т.д).
- доменные исключения — KeyNotExistException (идентификатор мероприятия не найден), NoAvailableSeatsException (нет доступных мест для бронирования).

Проект не зависит ни от чего внешнего.

# Application (EventsApi.Application)
Проект, содердит бизнес-логику и абстракции:
- интерфейсы сервисов и их реализации (use cases): IEventService (работа с событиями), IBookingService (работа с бронированием). Реализации интерфейсов: EventService, BookingService;
- интерфейсы портов — абстракции для доступа к данным (репозитории). Application определяет, что ему нужно от инфраструктуры (`EventsApi.Infrastructure`), через эти интерфейсы: IEventRepository, IBookingRepository;
- DTO (объекты передачи данных между слоями `EventsApi.Application` и `EventsApi.Presentation`);
- фоновые сервисы — Фоновый сервис для регистрации бронирования `BookingBackgroundService`;
- extension-метод для регистрации всех Application-зависимостей в DI-контейнере - `AddApplicationServices`.

Проект зависит только слоя Domain `(EventsApi.Domain)`.

# Infrastructure (EventsApi.Infrastructure)
Проект, содержит реализации, которые зависят от внешних технологий:

- реализации интерфейсов репозиториев с использованием DbContext (реализует интерфейсы слоя Application);
- DbContext — AppDbContext;
- конфигурации сущностей: EventConfiguration, BookingConfiguration;
- миграции БД;
- extension-метод для регистрации всех Infrastructure-зависимостей в DI-контейнере.

Проект зависит от Domain `(EventsApi.Domain)` и Application `(EventsApi.Application)`.

# Presentation (EventsApi.Presentation)
Веб-проект HTTP API.

Содержит:
- эндпоинты/контроллеры — что-бы получить HTTP-запрос, вызвать нужный обработчик/сервис в Application, вернуть ответ: EventsController (Контроллер для работы с мероприятиями) и BookingsController (Контроллер для работы с бронированием);
- обработчик глобальных исключений с маппингом доменных исключений в HTTP-статусы, ответ формируется в формате ProblemDetails (RFC 7807): GlobalExceptionHandlingMiddleware;
- composition root в Program.cs — регистрация всех зависимостей через extension-методы : AddInfrastructureServices(), AddApplicationServices(), AddPresentationServices().

Проект зависит от Infrastructure `(EventsApi.Infrastructure)` и Application `(EventsApi.Application)`.

## Cхема направления зависимостей проектов

![схема направления зависимостей проектов](https://pictures.s3.yandex.net/resources/image_1779201047.png)


---

## Ролевая модель и разграничение прав

Сервис бронирования использует **JWT-аутентификацию **


### Роли пользователей

| Роль | Описание | Права доступа |
|------|----------|---------------|
| `User` | Обычный пользователь | • Обычный пользователь может только бронировать события и отменять собственные брони. |
| `Admin` | Администратор | • Администратор управляет событиями — создаёт, редактирует, удаляет их и может отменять любые брони |

### Разграничение прав доступа к API

#### Публичные endpoints (без аутентификации)

```http
GET  /Events              # Список событий (с фильтрацией и пагинацией)
GET  /Events/{eventId}    # Информация о событии по идентификатору
POST /auth/register       # Регистрация нового пользователя
POST /auth/login          # Аутентификация и получение JWT-токена
```

#### Endpoints, требующие аутентификации

```http
POST    /Events/                      # Метод для создания события
PUT     /Events/{eventId}             # Метод обновления даных события
DELETE  /Events/{eventId}             # Метод удаляет событие по идентификатору
POST    /Events/{eventId}/book        # Метод для создания бронирования
GET     /Bookings/{bookingId}         # Получение информации по бронированию
DELETE  /Bookings/{bookingId}         # Отмена брони (только владелец или Admin)
```

**Ограничения для роли User:**
- запрет бронирования события, которое уже началось (`400 EventAlreadyStartedException`)
- запрет превышения лимита активных броней (по умолчанию 10) → `409 BookingLimitExceededException`
- запрет отмены чужой брони → `403 AccessDeniedException`

### Инструкция по получению JWT-токена через Swagger

1. **Регистрация пользователя**

   Откройте Swagger UI: [https://localhost:5091/swagger/index.html](https://localhost:5001/swagger/index.html)

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

| Claim | Значение | Описание |
|-------|----------|----------|
| `sub` (`Name`) | Логин пользователя | Используется для идентификации пользователя при бронировании |
| `role` | `User` или `Admin` | Для проверки роли в методах контроллера `[Authorize(Roles = "Admin")]` |
| `jti` | GUID | Уникальный идентификатор токена |
| `iat` | Unix timestamp | Время выдачи токена |

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

| Параметр | Тип | Описание |
|----------|-----|----------|
| `SchemeName` | string | Название схемы аутентификации |
| `Secret` | string | **Секретный ключ для подписи JWT** (минимум 32 символа для HMAC SHA-256) |
| `Issuer` | string | Издатель токена (проверяется при валидации) |
| `Audience` | string | Целевая аудитория токена (проверяется при валидации) |
| `Lifetime` | int | Время жизни токена в **минутах** |

---

### Рекомендации использовать безопасное значение в продакшене
1. **Используйте User Secrets (для локального тестирования):**
   ```bash
   dotnet user-secrets set "JwtSettings:Secret" "ProdSecretKey+!@#!!!"
   ```

2. **Используйте переменные окружения :**
   ```bash
   export JwtSettings_Secret="ProdSecretKey+!@#!!!"

### Хеширование паролей
Для реализации хеширования строки и проверки соответствия пароля хешу используется библиотека  BCrypt.Net.BCrypt

```c#
		public string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}
		
		public bool VerifyHashedPassword(string providedPassword, string hashedPassword)
		{
			return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
		}
```

# Необходимые компоненты для приложения
Проект разрабатывается на VS Code в ОС Linux с использованием ASP .NET Core 10

## Требования
В системе должен быть установлен пакет docker и docker-compose (для ОС Windows должен быть установлен Docker Desktop) для запуска контейнера с БД PostgreSQL для хранения данных.
В системе должен быть установлен пакет .NET 10 (для ОС Linux dotnet-sdk-10.0).


## Установка

1. Клонирем репозиторий:

```
https://github.com/SokolovPV/YandexPracticum.git
```

2. Сборка проекта

переходим дирректорию проекта решения и выполняем сборку проекта Presentation 

```
dotnet build EventsApi.Presentation
```
3. Запуск базы данных PostgreSQL в контейнере Docker
 
 перходим в директорию проекта решения и выполняем команду

для операционных систем семейства Linux
```
dotnet-compose up -d
```

для операционных систем семейства Windows
```
dotnet compose up -d
```

4. Запуск тестов

Запустите сборку проекта с Unit тестами

```
dotnet build EventsApi.UnitTest
```
Выполнение Unit тестов

```
dotnet test EventsApi.UnitTest
```


Запустите сборку проекта с Интеграционными тестами
```
dotnet build EventsApi.IntegrationTests
```

Выполнение интеграционных тестов

```
dotnet test EventsApi.IntegrationTests
```

5. Запуск WebApi решения
   
```
dotnet run --project EventsApi.Presentation
```

#### Для использования API переходим в браузере по адресу http://localhost:5091/swagger/index.html

## Описание моделей данных

### User

Модель пользователя системы

#### Описание модели пользователя

| Поле          | Тип                | Обязательное | Описание                   |
| ------------- | ------------------ | ------------ | -------------------------- |
| `Id`          | `Guid`             | Да           | Идентификатор пользователя |
| `Login`       | `string`           | Да           | Имя пользователя          | 
| `PasswordHash`| `string`           | Да           | Хэш пароля пользователя    |
| `Role`        | `RoleType`         | Да           | Роль пользователя          |

#### Роли пользователя RoleType

| Поле        | Описание                         |
| ----------- | -------------------------------- |
| `User`      | Роль простого пользователя       |
| `Admin`     | Роль администратора              |


### Event

Модель представляет событие которое хранится в БД PostreSQL

#### Описание модели события

| Поле             | Тип        | Обязательное | Описание                          |
| ---------------- | ---------- | ------------ | --------------------------------- |
| `Id`             | `Guid`     | Да           | Идентификатор события             |
| `Title`          | `string`   | Да           | Название события                  |
| `Description`    | `string`   | Нет          | Описание события                  |
| `StartAt`        | `DateTime` | Да           | Дата начала события               |
| `EndAt`          | `DateTime` | Нет          | Дата окончания события            |
| `TotalSeats`     | `int`      | Да           | Общее количество мест на событие  |
| `AvailableSeats` | `int`      | Да           | Текущее количество свободных мест |

### Booking

Модель представляет бронирование которое хранится в БД PostreSQL

#### Описание модели бронирования

| Поле          | Тип                | Обязательное | Описание                                                          |
| ------------- | ------------------ | ------------ | ----------------------------------------------------------------- |
| `Id`          | `Guid`             | Да           | Идентификатор бронирования                                        |
| `EventId`     | `string`           | Да           | Идентификатор события, к которому относится бронирование          |
| `UserId`      | `Guid`             | Да           | Идентификатор пользователя,кто забронировал мероприятие           |
| `Status`      | `BookingStatus`    | Да           | Текущий статус бронирования (при создании по умолчанию = Pending) |
| `CreatedAt`   | `DateTime`         | Да           | Дата создания бронирования                                        |
| `ProcessedAt` | `DateTime \| null` | Нет          | Дата обработки бронирования                                       |

#### Статус бронирования BookingStatus

| Поле        | Описание                         |
| ----------- | -------------------------------- |
| `Pending`   | Бронь создана, ожидает обработки |
| `Confirmed` | Бронь подтверждена               |
| `Rejected`  | Бронь отклонена ситемой          |
| `Cancelled` | Бронь отклонена пользователем    |

## Методы для работы с пользователями

<details>
<summary>Создание нового пользователя</summary>

- **Метод:** `POST`
- **URL:** `/auth/register`
- **Параметры запроса:**
  - `login` (обязательно) - имя пользователя 
  - `password` (обязательно) - пароль пользователя 
  - `role` (обязательно) - роль пользователя (0 - простой пользователь, 1 - администратор)

- **Пример запроса:**

```bash
curl -X 'POST' \
  'http://localhost:5091/auth/register' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "login": "ivan_ivanov",
  "password": "Aqswdefr",
  "role": "1"
}'
```
- **Пример ответа**

```json
```

</details>

<details>
<summary>Получение токена доступа пользователя</summary>

- **Метод:** `POST`
- **URL:** `/auth/login`
- **Параметры запроса:**
  - `login` (обязательно) - имя пользователя 
  - `password` (обязательно) - пароль пользователя 

 **Пример запроса:**

```bash
curl -X 'POST' \
  'http://localhost:5091/auth/login' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "login": "ivan_ivanov",
  "password": "Aqswdefr"
}'
```
- **Пример ответа**

```json
```

</details>

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
  'http://localhost:5091/Events' \
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
  'http://localhost:5091/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
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
  'http://localhost:5091/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
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
  'http://localhost:5091/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
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
  'http://localhost:5091/Events/43623902-6ef0-4e54-9e0d-0973e780bede' \
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

## Операции с Бронированием

Бронирование реализовано в фоновом сервисе, который опрашивает репозиторий с созданныйи бронированиями, и переводит их в статус "подтверждено" или в случае отмены бронирования "отклонено". Сервис обрабатывает события один раз в 5 секунд.

- Создаем событие

```bash
curl -X 'POST' \
  'https://localhost:5091/Events' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-11T07:54:33.751Z",
  "endAt": "2026-04-13T07:54:33.751Z"
}'
```

Запрос для создания события. Из него берём идентификатор события.

```json
{
  "id": "df00f0e9-2554-4c0e-b76f-c751ba8870fc",
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-11T07:54:33.751Z",
  "endAt": "2026-04-13T07:54:33.751Z"
}
```

- Запрос для создания бронирования

```bash
curl -X 'POST' \
  'https://localhost:5091/Events/df00f0e9-2554-4c0e-b76f-c751ba8870fc/book' \
  -H 'accept: */*' \
  -d ''
```

из ответа забираем ID бронирования

```json
{
  "id": "07c5ba9a-900a-43ba-931f-ee5f9ec58d79",
  "eventID": "df00f0e9-2554-4c0e-b76f-c751ba8870fc",
  "status": "Pending"
}
```

- Через 5 секунд проверяем статус бронирования

```bash
curl -X 'GET' \
  'https://localhost:5091/Bookings/07c5ba9a-900a-43ba-931f-ee5f9ec58d79' \
  -H 'accept: */*'
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
  'https://localhost:5091/Events/07c5ba9a-900a-43ba-931f-ee5f9ec58d79/book' \
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

### Формат ошибок ProblemDetails (RFC 7807) для бронирования
**Примеры ответа ошибка неправильный идентификатор**
```json
{
  "type": "Invalid Identifier",
  "status": 404,
  "detail": "Идентификатор бронирования не найден."
}
```

**Пример ответа при создании бронирования если недостаточно количество свободных мест**
```json
{
  "type": "No available seats",
  "status": 409,
  "detail": "Для события ID=f1981d77-e952-420f-b9a1-13a8e2efcf8f отстутствуют свободные места для бронирования."
}
```

## Добавление миграции БД

### Создание миграции
Для создания миграции необходимо выпонить команду 
```bash
dotnet ef migrations add InitialCreate --project EventsApi.Infrastructure --startup-project EventsApi.Presentation
```
- `InitialCreate` - имя миграции
- `EventsApi.Infrastructure` - проект с БД и конфигурацией
- `EventsApi.Presentation` - проект со строкой подключения к БД

### Применение миграции к БД

``` bash
dotnet ef database update --project EventsApi.Infrastructure --startup-project EventsApi.Presentation
```

- `EventsApi.Infrastructure` - проект с БД и конфигурацией
- `EventsApi.Presentation` - проект со строкой подключения к БД
