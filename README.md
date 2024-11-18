**Image Signing System**

**Описание проекта**

Проект представляет собой сервис для подписания изображений внутри организации. Сервис позволяет пользователю загружать изображения, которые проверяются и подписываются поддержкой и администраторами. Подписанные изображения могут быть использованы для подтверждения их подлинности.

**Основные роли**
1. Администратор: Подтверждает и подписывает изображения. Имеет доступ только к изображениям, переданным ему поддержкой.
2. Поддержка: Принимает изображения от пользователей, проверяет заявки и добавляет метаинформацию. Видит список заявок пользователей и может забирать их в работу. Заявка пропадает из списка поддержки, как только кто-то из команды взял её в обработку. Поддержка видит только те изображения, которые передала администратору.
3. Пользователь: Загружает изображения для подписания, получает подписанные изображения и может проверять подлинность подписей на загруженных изображениях.

**Основной функционал**
1) Загрузка изображения пользователем:
        Пользователь загружает изображение для подписи.
        Поддержка получает уведомление о новой заявке.
2) Обработка заявки и добавление метаинформации:
        Поддержка видит доступные заявки от пользователей и может выбрать их для обработки (заявка пропадает из списка у других пользователей поддержки).
        После проверки изображения поддержка добавляет метаинформацию, необходимую для подписи, и передает заявку администратору.
4) Подписание изображения администратором:
        Администратор сверяет метаинформацию и подписывает изображение.
        Подписанное изображение возвращается поддержке для отправки пользователю.
5) Проверка подписи:
        Пользователь может загрузить изображение для проверки подлинности. Система, используя метаинформацию, встроенную в изображение, сверяет его подлинность.
        Если изображение подписано, пользователю возвращается результат проверки.
        Если изображение не подписано или требуется дополнительная проверка, изображение отправляется в обработку поддержке.

**Структура API**

POST /api/upload - загрузка изображения пользователем.

GET /api/support/requests - получение списка заявок для поддержки.

POST /api/support/requests/{id} - добавление метаинформации и передача изображения администратору.

POST /api/admin/sign/{id} - подписание изображения администратором.

GET /api/user/check-signature - проверка подписи на изображении.

**Технологии**

Backend: ASP.NET Web API

Authentication: JWT для аутентификации и авторизации ролей

Хранение данных: Только информация о заявках, которые ещё не обработаны; подписанные изображения и метаинформация хранятся только до момента их передачи пользователю. Подпись и метаданные сохраняются в самом изображении.

**Установка и запуск**

        git clone <url репозитория>
        cd <project_folder>
        dotnet restore
        dotnet run

**Структура проекта:**

/Controllers - контроллеры для API-эндпоинтов.

/Data - мост для сущностей в БД.

/Dto - объекты Dto.

/Entity - модели для данных (например, модели для пользователя, заявки, изображения и подписи).

/Extensions - доп расширения.

/Interfaces - интрефейсы.

/Mapping - AutoMapper.

/Middleware - хранение middleware компонентов.

/Repositories - классы для работы с данными (например, с базой данных или файловым хранилищем).

/Services - сервисы для работы с бизнес-логикой, включая создание и проверку подписей.


**ПЛАН ПРОЕКТА:**

Неделя 1: Подготовка структуры проекта и базовая настройка API

Неделя 2: Реализация логики подписи и проверки изображений

Неделя 3: Подписание изображения администратором

Неделя 4: Настройка Digest-аутентификации

Неделя 5: Функционал для загрузки изображений пользователем

Неделя 6: Обработка заявок и передача администратору

Неделя 7: Проверка подписей пользователем

Неделя 8: Логирование и обработка ошибок

Неделя 9: Оптимизация и рефакторинг

Неделя 10: Финальное тестирование