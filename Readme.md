# VGoalsPlanner

Персональный планировщик целей и задач. Пользователь строит иерархию **целей**
(`VGoal`), внутри целей заводит листовые **задачи** (`VTask`), расписывает их
по дням (`VDay`), ведёт журнал выполнения (`VTaskLog`) и быстрые заметки
(`VNotification`). Все изменения фиксируются в общем аудит-логе (`AuditLog`).

## Модель предметной области

- **`VGoal`** — цель. Может быть вложена в другую цель (`ParentGoalId`) и
  содержать дочерние цели и/или задачи. Хранит `Priority`, `Status`
  (`VGoalStatus`), мягкое удаление (`IsDeleted`).
- **`VTask`** — листовая единица планирования, всегда принадлежит ровно одной
  цели (`GoalId` обязателен). Хранит `Priority`, `Status` (`VTaskStatus`),
  `Type` (`VTaskType`), теги, дедлайн, `StartedAt`/`CompletedAt`,
  `CompletedPercent`, мягкое удаление.
- **`VDay`** — календарный день пользователя (`Date` + `SpecialNote`).
  Существует лениво: строка в БД создаётся только когда к дню что-то
  привязывают (задачу, заметку, лог) или явно запрашивают.
- **`VDayVTask`** — связь many-to-many между днём и задачей с собственным
  `OrderIndex` (задача может быть запланирована на несколько дней, в каждом —
  со своей позицией).
- **`VNotification`** — быстрая заметка на конкретный день (`IsRead`,
  `IsCompleted`), удаляется полностью (без мягкого удаления).
- **`VTaskLog`** — запись в журнале выполнения задачи: заметка, вложения,
  потраченное время, `OldStatus`/`NewStatus` как метаданные (сам статус
  задачи запись лога не меняет — это отдельная команда).
- **`AuditLog`** — системный журнал изменений (кто/что/когда), пишется
  автоматически при `SaveChanges`.

## Структура решения

Clean Architecture, слои зависят только "вниз":

| Проект | Содержит | Зависит от |
|---|---|---|
| `VGoalsPlanner.Domain` | Сущности, enum'ы, `Result`/`Error` (Result-pattern), ошибки по фичам | — |
| `VGoalsPlanner.Application` | CQRS-команды/запросы (MediatR), FluentValidation-валидаторы, DTO, организовано по Vertical Slices (`VGoals/`, `VTasks/`, `VDays/`, `VNotifications/`, `VTaskLogs/`) | Domain |
| `VGoalsPlanner.Infrastructure` | `VGoalsPlannerDbContext`, EF Core Fluent API конфигурации, миграции | Domain, Application |
| `VGoalsPlanner.API` | ASP.NET Core контроллеры, `Program.cs`, DI | Application, Infrastructure |
| `VGoalsPlanner.UIReact` | React SPA-клиент (см. ниже) | — (обращается к API по HTTP) |

`VGoalsPlanner.UIReact` — обычная соседняя папка, не входит в
`VGoalsPlanner.slnx` (у Vite-проекта нет `.csproj`).

## Технологии

**Backend:** .NET 10, ASP.NET Core Web API, Entity Framework Core 10
(Npgsql/PostgreSQL), MediatR + FluentValidation (CQRS), Result-pattern вместо
исключений для бизнес-ошибок.

**Frontend:** React 19, Vite 6, TypeScript.

Swagger/OpenAPI пока не подключён.

## API (контроллеры)

Все — `api/[controller]`, ошибки — JSON `{ code, description }` (`VGoal.NotFound`
и т.п.; коды с `NotFound` маппятся в HTTP 404, остальные — в 400).

- **`VGoalsController`** (`api/vgoals`) — CRUD целей, дерево (ленивая загрузка
  по уровню: `roots` + `{id}` с одним уровнем детей), смена статуса,
  перемещение по иерархии, каскадное мягкое удаление поддерева.
- **`VTasksController`** (`api/vtasks`) — CRUD задач, список по цели, поиск
  по названию/описанию/тегам, смена статуса (авто `StartedAt`/`CompletedAt`),
  перенос в другую цель, сортировка внутри цели.
- **`VDaysController`** (`api/vdays`) — расписание на день/неделю/месяц,
  привязка/отвязка задачи ко дню, сортировка задач внутри дня, заметка дня,
  список незапланированных задач.
- **`VNotificationsController`** (`api/vnotifications`) — заметки дня,
  непрочитанные, пометка прочитано/выполнено, полное удаление.
- **`VTaskLogsController`** (`api/vtasklogs`) — журнал по задаче, дневной
  отчёт, отчёт по времени за период, агрегированный прогресс по цели.

Ещё не реализован: `AuditController` (просмотр `AuditLog`).

## Как запустить

### Предварительно

- .NET 10 SDK
- PostgreSQL (локально)
- Node.js (для фронтенда)

### Backend

1. Задать строку подключения (общий PostgreSQL на `localhost:5432`, база `vgplannerdb`)
   в user-secrets, ключ `ConnectionStrings:GoalPlannerDbConnection` — пароль не хранится
   в `appsettings.Development.json`:
   ```bash
   dotnet user-secrets set "ConnectionStrings:GoalPlannerDbConnection" "Host=localhost;Port=5432;Database=vgplannerdb;Username=postgres;Password=<password>" --project VGoalsPlanner.API
   ```
   (в Visual Studio: правый клик по `VGoalsPlanner.API` → Manage User Secrets).
2. Запустить:
   ```bash
   dotnet run --project VGoalsPlanner.API
   ```
   Вне `Production` окружения миграции EF Core применяются автоматически при
   старте. API слушает `http://localhost:5016` (и
   `https://localhost:7135` в https-профиле) — см.
   `VGoalsPlanner.API/Properties/launchSettings.json`.

### Frontend

```bash
cd VGoalsPlanner.UIReact
npm install
npm run dev
```

Открывается на `http://localhost:5173`. Dev-сервер Vite проксирует `/api/*`
на `http://localhost:5016` (см. `vite.config.ts`), поэтому CORS на бэкенде не
настроен и не нужен — для работы фронтенда нужно, чтобы **оба** процесса были
запущены одновременно. Прокси-сервер явно слушает `127.0.0.1` — на некоторых
машинах Node резолвит `localhost` только в IPv6 (`::1`), из-за чего порт
становится недоступен по IPv4.

## Текущий статус

**Готово:** домен и EF-конфигурации для всех сущностей, миграции; полный
CQRS-слой и контроллеры для Goals/Tasks/Days/Notifications/TaskLogs; React
шаблон с одним экраном — `GoalsAndTaskView` (ленивое дерево целей/задач,
только чтение).

**Не готово:** `AuditController`; мутации (create/update/delete/move) во
фронтенде — сейчас только просмотр дерева; роутинг и состояние приложения на
фронтенде не заводились — экран один, поверх localState.
