OrgDirectory (.NET 8, ASP.NET Core MVC) — Docker Compose

Справочники организаций и сфер деятельности с CRUD, группировкой по сфере, сортировкой по клику и экспортом в XML/JSON/CSV. Есть авторизация (логин/регистрация) и модель гражданина (директор как FK).

Запуск:
    docker compose up -d --build
    В корне лежат: docker-compose.yml, Dockerfile, .env.


Вход:
(http://localhost:5000/Auth/Login)

Регистрация через меню(аккаунта по умолчанию нет,свой надо будет создать)

При первом запуске EF создаст БД и засеет демо-данные (несколько сфер, граждан и организаций).

Что реализовано :

 Страница организаций с группировкой по сфере деятельности

 Страница справочника сфер деятельности

 Добавление / удаление / редактирование в обоих справочниках

 (*) Сортировка по клику на заголовок (asc/desc)

 (*) Экспорт в xml/json/csv

 (*) Авторизация (логин/регистрация)

 (*) Гражданин и замена ФИО директора на FK


Навигация:

/Activities — сферы деятельности (список, создание, редактирование, удаление, экспорт)

/Organizations — организации (сгруппированы по сфере; CRUD, сортировка, экспорт)

/Citizens — граждане (CRUD, экспорт; в организации директор указывается как Citizen)

/Auth/Login, /Auth/Register, /Auth/Logout — авторизация

Сортировка:

Клик по заголовку меняет направление. В URL: ?sortField=<поле>&sortDir=asc|desc.

Экспорт:

/Activities/Export?fmt=xml|json|csv
/Organizations/Export?fmt=xml|json|csv
/Citizens/Export?fmt=xml|json|csv

Сделано через Strategy — новый формат = новая стратегия + регистрация в DI.

Фронтенд:

Подход: “SPA-lite” на Razor Views. Навигация и таблицы работают реактивно за счёт AJAX и частичных представлений, без полноценных фронтенд-фреймворков.
(*)Bootstrap 5
(*)ASP.NET Core MVC + Razor (Views/Partials)

Единый лэйаут: Views/Shared/_Layout.cshtml
Подключает Bootstrap, отрисовывает шапку
Частичные Index-страницы: Views/*/Index.cshtml
При обычном заходе рендерятся с лэйаутом; при AJAX-навигации отдают только тело.
Таблицы в partial-вьюхах: _OrganizationsTable.cshtml, _CitizensTable.cshtml, _ActivitiesTable.cshtml
Внутри — .table-wrap с data-current-field/dir для сортировки.


Порождающие

Factory  — создание настроенного HttpClient для токен-сервиса.
: Program.cs → AddHttpClient<ITokenService, TokenService>(...).

Factory(Resolver) для экспорта — выбор реализации экспорта по fmt=csv|json|xml.
: Program.cs → регистрация IExportResolver<> и IExportStrategy<> (Json/Xml/Csv).

Singleton (lifetime через DI) — стратегии экспорта зарегистрированы как Singleton.
: Program.cs → AddSingleton(typeof(IExportStrategy<>), ...).

Структурные

Facade — единая обёртка над Go-auth сервисом (login/refresh/register), прячет HTTP-детали.
: TokenService.cs (+ интерфейс ITokenService.cs).

Repository (Generic Repository + специализированные) — изоляция доступа к EF Core, include/фильтры/сортировка.
: IRepository.cs, Repository.cs, ActivityRepository.cs, OrganizationRepository.cs, CitizenRepository.cs.

DTO / Adapter-подобные проекции — доменные сущности проецируются в экспортные/вью-модели.
: TokenDtos.cs, RegisterViewModel.cs, экспортные DTO (регистрация стратегий в Program.cs).

Поведенческие

Strategy — семейство стратегий экспорта (CSV/JSON/XML) и выбор стратегии резолвером.
: Program.cs (регистрация стратегий/резолвера), вызовы Export в контроллерах.

Chain of Responsibility  — обработка запроса конвейером: авто-refresh токена → аутентификация → авторизация.
: TokenRefreshMiddleware.cs, Program.cs → app.UseMiddleware<TokenRefreshMiddleware>(); app.UseAuthentication(); app.UseAuthorization();

Авторизация,регистрация,аутенфикация (внешний самописный Go-сервис):


После логина/регистрации веб кладёт access_token и refresh_token в HttpOnly-cookies; TokenRefreshMiddleware автоматом обновляет access-токен через эндпоинт refresh Go-сервиса.

URL сервиса задаётся через .env/compose переменную Auth__ServiceBaseUrl (по умолчанию: http://auth-service:7001).

Подпись токенов:

HS256 — общий секрет в .env: AUTH_SECRET.

Проверка токена настроена на соответствие AUTH_ISSUER и AUTH_AUDIENCE из .env.

