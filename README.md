# AspNetJWTAuth

## Описание

Этот проект представляет собой серверную часть на ASP.NET Core (C#), реализующую JWT-аутентификацию, управление пользователями и ролями, а также работу с refresh-токенами. В проекте используется Entity Framework Core для работы с базой данных и реализована проверка email через SMTP.

## Основные возможности

- Регистрация и аутентификация пользователей с использованием JWT
- Управление ролями пользователей
- Хранение и обновление refresh-токенов
- Проверка email-адреса через SMTP
- Обработка ошибок через middleware

## Структура проекта

- `src/Config/` — настройки JWT и refresh-токенов
- `src/Controllers/` — контроллеры для аутентификации, ролей и пользователей
- `src/Data/` — контекст базы данных и репозитории
- `src/DTOs/` — DTO для передачи данных
- `src/Interfaces/` — интерфейсы сервисов
- `src/Middleware/` — middleware для обработки ошибок
- `src/Models/` — модели пользователей и refresh-токенов
- `src/Services/` — сервисы, включая работу с токенами
- `src/Utils/checkEmailSMTP.cs` — утилита для проверки email через SMTP

## Управление секретными данными

Для обеспечения безопасности важно не хранить чувствительную информацию (строки подключения к БД и JWT-ключи) в файлах, которые могут попасть в систему контроля версий.

### Использование User Secrets

В разработке используйте механизм [User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) для безопасного хранения:

1. Инициализируйте User Secrets для проекта:
   ```sh
   dotnet user-secrets init --project "<путь_к_вашему_проекту>"
   ```

2. Добавьте строку подключения к БД:
   ```sh
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<адрес_сервера>;Port=<порт>;Database=<имя_бд>;Username=<пользователь>;Password=<пароль>;Include Error Detail=true;Maximum Pool Size=20;Minimum Pool Size=5;Connection Lifetime=15;Pooling=true;Trust Server Certificate=true" --project "<путь_к_вашему_проекту>"
   ```

3. Добавьте настройки JWT:
   ```sh
   dotnet user-secrets set "JwtSettings:SecretKey" "<ваш_секретный_ключ>" --project "<путь_к_вашему_проекту>"
   dotnet user-secrets set "JwtSettings:ExpiryMinutes" "<срок_действия_в_минутах>" --project "<путь_к_вашему_проекту>"
   dotnet user-secrets set "JwtSettings:Issuer" "<наименование_издателя>" --project "<путь_к_вашему_проекту>"
   dotnet user-secrets set "JwtSettings:Audience" "<целевая_аудитория>" --project "<путь_к_вашему_проекту>"
   ```

4. Добавьте настройки RefreshToken:
   ```sh
   dotnet user-secrets set "RefreshTokenSettings:ExpiryDays" "<срок_действия_в_днях>" --project "<путь_к_вашему_проекту>"
   dotnet user-secrets set "RefreshTokenSettings:TokenSizeBytes" "<размер_токена_в_байтах>" --project "<путь_к_вашему_проекту>"
   dotnet user-secrets set "RefreshTokenSettings:MaxActiveTokensPerUser" "<максимальное_число_активных_токенов>" --project "<путь_к_вашему_проекту>"
   ```

5. Проверить список всех секретов:
   ```sh
   dotnet user-secrets list --project "<путь_к_вашему_проекту>"
   ```

### Расположение секретов

User Secrets хранятся в специальном файле вне репозитория:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

## Важные замечания по проверке email

Проверка email реализована через SMTP-запросы (см. `src/Utils/checkEmailSMTP.cs`). Некоторые почтовые сервисы могут блокировать такие проверки, что может привести к ложным результатам. Для корректной работы рекомендуется создать локальный почтовый сервер (например, с помощью [hMailServer](https://www.hmailserver.com/)) и использовать собственный почтовый ящик для тестирования.

**Внимание!** В коде захардкожен домен `test.local`. Для полноценной работы рекомендуется создать соответствующий домен и почтовый ящик на локальном сервере.

## Запуск проекта

1. Установите .NET 8.0 SDK
2. Настройте секреты как описано выше или используйте `appsettings.json`
3. (Рекомендуется) Настройте локальный почтовый сервер и создайте домен `test.local`
4. Соберите и запустите проект:
   ```sh
   dotnet build
   dotnet run
   ```

## Миграции базы данных

Для применения миграций используйте:
```sh
dotnet ef database update
```