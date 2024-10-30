**Image Signing System**
**Описание проекта**
Проект представляет собой сервис для подписания изображений, предназначенный для использования внутри организации. Сервис позволяет пользователю загружать изображения, которые проверяются и подписываются поддержкой и администраторами. Подписанные изображения можно использовать для подтверждения их подлинности.

**Основные роли**
Администратор: Подтверждает и подписывает изображения. Имеет доступ только к изображениям, переданным ему поддержкой.
Поддержка: Принимает изображения от пользователей, проверяет заявки и добавляет метаинформацию. Может видеть только те изображения, которые передала администратору.
Пользователь: Загружает изображения для подписания, получает подписанные изображения и может проверять подпись на загруженных изображениях.

**Основной функционал**
1) Загрузка изображения пользователем:
        Пользователь загружает изображение для подписи.
        Поддержка получает уведомление о новой заявке.
2) Проверка и добавление метаинформации:
        Поддержка просматривает изображение, добавляет метаинформацию и передает изображение администратору для подписи.
3) Подписание изображения администратором:
        Администратор сверяет метаинформацию и подписывает изображение.
        Подписанное изображение возвращается поддержке для отправки пользователю.
4) Проверка подписи:
        Пользователь может загрузить изображение для проверки подписи. Система сравнивает подпись с хранимыми данными и сообщает о результате.

**Структура API**
POST /api/upload - загрузка изображения пользователем.
GET /api/support/requests - получение списка заявок для поддержки.
POST /api/support/requests/{id} - добавление метаинформации и передача изображения администратору.
POST /api/admin/sign/{id} - подписание изображения администратором.
GET /api/user/check-signature - проверка подписи на изображении.

**Технологии**
Backend: ASP.NET Web API
Authentication: JWT для аутентификации и авторизации ролей
Хранение данных: Возможно использование базы данных (например, SQLite для прототипа) для хранения информации о заявках, ролях и изображениях.
**
Установка и запуск**
Склонировать репозиторий: git clone <url репозитория>
Перейти в папку проекта и установить зависимости: 
cd <project_folder>
dotnet restore
Запустить приложение: dotnet run

**Структура проекта:**
/Controllers - контроллеры для API-эндпоинтов.
/Entity - модели для данных (например, модели для пользователя, заявки, изображения и подписи).
/Dto - объекты Dto.
/Services - сервисы для работы с бизнес-логикой, включая создание и проверку подписей.
/Repositories - классы для работы с данными (например, с базой данных или файловым хранилищем).
