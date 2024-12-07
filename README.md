<h1 align='center'>Image Signing System</h1>

<h2>Описание проекта</h2>

<p>Проект представляет собой сервис для подписания изображений внутри организации. Сервис позволяет пользователю загружать изображения, которые проверяются и подписываются поддержкой и администраторами. Подписанные изображения могут быть использованы для подтверждения их подлинности.</p>

<h2>Основные роли</h2>
<ol>
  <li>
    <strong>Администратор:</strong>
    <p>Подтверждает и подписывает изображения. Имеет доступ только к изображениям, переданным ему поддержкой.</p>
  </li>
  <li>
    <strong>Поддержка:</strong>
    <p>Принимает изображения от пользователей, проверяет заявки. Видит список заявок пользователей и может забирать их в работу. Заявка пропадает из списка поддержки, как только кто-то из команды взял её в обработку. Поддержка видит только те изображения, которые получила от пользователя.</p>
  </li>
  <li>
    <strong>Пользователь:</strong>
    <p>Загружает изображения для подписания, получает подписанные изображения и может проверять подлинность подписей на загруженных изображениях, а также скачивать и удалять свои изображения.</p>
  </li>
</ol>

<h2>Основной функционал</h2>
<ol>
  <li>
    <strong>Загрузка изображения пользователем:</strong>
    <p>Пользователь загружает изображение для подписи. Поддержка получает уведомление о новой заявке.</p>
  </li>
  <li>
    <strong>Обработка заявки и добавление метаинформации:</strong>
    <p>Поддержка видит доступные заявки от пользователей и может выбрать их для обработки (заявка пропадает из списка у других пользователей поддержки). После проверки изображения передает заявку администратору.</p>
  </li>
  <li>
    <strong>Подписание изображения администратором:</strong>
    <p>Администратор сверяет метаинформацию и подписывает изображение. Подписанное изображение возвращается обратно пользователю со статусом Signed.</p>
  </li>
  <li>
    <strong>Проверка подписи:</strong>
    <p>Пользователь может загрузить изображение для проверки подлинности. Система, используя метаинформацию, встроенную в изображение, сверяет его подлинность. Если изображение подписано, пользователю возвращается результат проверки. Если изображение не подписано, сервис отправляет клиенту результат об этом.</p>
  </li>
</ol>

<h2>Технологии</h2>

<b>Backend:</b> ASP.NET 8 Web API
<ul>
  <li>User Manager для работы с пользователями в БД</li>
  <li>Общение с клиентом посредством JSON объектов</li>
  <li>Entity Framework для общения с БД</li>
  <li>MailKit для работы сервиса нотификации</li>
  <li>ImageSharp для работы с изображениями</li>
  <li>AutoMapper для маппинга данных между DTO и Entity</li>
</ul>

<b>Authentication:</b> Digest для аутентификации и авторизации ролей

<b>Подпись изображений:</b> RSA с использованием private и public keys

<b>Хранение данных:</b> SQL Server: Информация о заявках, которые ещё не обработаны; подписанные изображения и метаинформация хранятся только до момента их скачивания или удаления пользователем. Подпись и метаданные сохраняются в самом изображении.

<b>Frontend:</b> JS React

<b>Также использованы:</b>
<ul>
  <li>UnitOfWork pattern</li>
  <li>Dependecy Injection</li>
  <li>Repository Pattern</li>
  <li>Service Pattern</li>
  <li>Middleware</li>
</ul>

<h2>Структура API</h2>

<ul>
  <li>
    <h3>Administrator</h3>
    <ul>
      <li>GET /Admin/get-admin-images</li>
      <li>POST /Admin/sign/{imageId}</li>
      <li>POST /Admin/reject-signing/{imageId}</li>
      <li>GET /Admin/get-users</li>
      <li>GET /Admin/get-support</li>
      <li>GET /Admin/get-admins</li>
      <li>GET /Admin/view-image/{imageId}</li>
      <li>POST /Admin/change-role/{userId}</li>
      <li>POST /Admin/send-email</li>
      <li>DELETE /Admin/remove-user/{userId}</li>
    </ul>
  </li>
  
  <li>
    <h3>Auth</h3>
    <ul>
      <li>POST /Account/Registration</li>
      <li>POST /Account/Login</li>
      <li>OPTIONS /Account/Login</li>
      <li>GET /Account/LoginNonce</li>
      <li>DELETE /Account/delete-user/{userId}</li>
      <li>PUT /Account/change-data</li>
      <li>GET /Account/get-data</li>
    </ul>
  </li>
  
  <li>
    <h3>Support</h3>
    <ul>
      <li>GET /Support/get-support-images</li>
      <li>POST /Support/request-signature/{imageId}</li>
      <li>POST /Support/reject-signing/{imageId}</li>
      <li>GET /Support/view-image/{imageId}</li>
    </ul>
  </li>
  
  <li>
    <h3>User</h3>
    <ul>
      <li>GET /User/get-user-images</li>
      <li>GET /User/signed-images</li>
      <li>GET /User/rejected-images</li>
      <li>POST /User/upload</li>
      <li>POST /User/force-upload</li>
      <li>GET /User/download/{id}</li>
      <li>GET /User/verify-signature/{imageId}</li>
      <li>GET /User/get-signature/{imageId}</li>
      <li>DELETE /User/delete-image/{id}</li>
      <li>GET /User/get-user-data</li>
      <li>GET /User/view-image/{imageId}</li>
    </ul>
  </li>

  <li>
    <h3>Unauthorized</h3>
    <ul>
      <li>POST /Unauthorized/find-signature</li>
      <li>POST /Unauthorized/verify-file-signature</li>
    </ul>
  </li>
</ul>


<h2>Установка и запуск</h2>
<h3>API:</h3>

        git clone <url репозитория>
        cd <project_folder>
        dotnet ef migrations add InitialCreateMigration
        dotnet ef database update
        dotnet run
        
<h3>Frontend:</h3>

        npm start

<h2>Структура проекта:</h2>

<ul>
  <li>
    <strong>/Controllers</strong> - контроллеры для API-эндпоинтов.
  </li>
  <li>
    <strong>/Data</strong> - мост для сущностей в БД.
  </li>
  <li>
    <strong>/Dto</strong> - объекты Dto.
  </li>
  <li>
    <strong>/Entity</strong> - модели для данных (например, модели для пользователя, заявки, изображения и подписи).
  </li>
  <li>
    <strong>/Extensions</strong> - доп расширения.
  </li>
  <li>
    <strong>/Helpers</strong> - UnitOfWork и MD5Hash.
  </li>
  <li>
    <strong>/Interfaces</strong> - интерфейсы.
  </li>
  <li>
    <strong>/Keys</strong> - ключи шифрования.
  </li>
  <li>
    <strong>/Mapping</strong> - AutoMapper.
  </li>
  <li>
    <strong>/Middleware</strong> - хранение middleware компонентов.
  </li>
  <li>
    <strong>/Repositories</strong> - классы для работы с данными (например, с базой данных или файловым хранилищем).
  </li>
  <li>
    <strong>/Services</strong> - сервисы для работы с бизнес-логикой.
  </li>
</ul>

<h2>ПЛАН ПРОЕКТА:</h2>

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
