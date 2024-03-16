# Документация по API

## Описание
Это API предоставляет функционал для взаимодействия с базой данных новостей. Пользователи могут получать новостные статьи, искать конкретные новости, управлять категориями, ставить лайки статьям, добавлять/удалять избранное и получать персонализированную ленту новостей.

## Получить API-ключ
Можно в нашем боте: t.me/intelio_bot

## Базовый URL
https://a24916-27c7.w.d-f.pw

## Аутентификация
Все конечные точки требуют параметр api-key в строке запроса для аутентификации.

### GET-запросы

#### Получить все новости
- Запрос: /api/news/all?api={api-key}
- Описание: Получить все доступные новостные статьи в базе данных.

#### Получить последние N новостей
- Запрос: /api/news/last?api={api-key}&count={int}
- Описание: Получить указанное количество последних новостей.

#### Поиск новостей по ключевому слову
- Запрос: /api/news/search?api={api-key}&contains={string}
- Описание: Поиск новостей, содержащих указанный текст.

#### Поиск новостей по дате
- Запрос: /api/news/search/date?api={api-key}&date={string}
- Описание: Поиск новостей по определенной дате (формат: "yyyy-MM-dd").

#### Получить категории новостей
- Запрос: /api/news/category?api={api-key}
- Описание: Получить все категории новостей в базе данных.

#### Поиск новостей по категории
- Запрос: /api/news/category/search?api={api-key}&selected={string}
- Описание: Получить новости из выбранной категории.

#### Получить новость по ID
- Запрос: /api/news/{int}?api={api-key}
- Описание: Получить одну новостную статью по уникальному идентификатору.

#### Получить ID последней новости
- Запрос: /api/news/latest?api={api-key}
- Описание: Получить идентификатор последней новости в базе данных.

#### Получить новости за сегодня
- Запрос: /api/news/today?api={api-key}
- Описание: Получить новости за текущий день.

#### Лайк/Дизлайк новости
- Базовый запрос: /api/news/rate?api={api-key}
    - Поставить лайк: /api/news/rate?api={api-key}&userId={string}&newsId={int}&actions=set
    - Получить статус лайка: /api/news/rate?api={api-key}&userId={string}&newsId={int}&actions=get
    - Убрать лайк: /api/news/rate?api={api-key}&userId={string}&newsId={int}&actions=del

#### Избранное
- Базовый запрос: /api/news/favorites?api={api-key}
    - Получить избранное пользователя: /api/news/favorites?api={api-key}&userId={string}&actions=get
    - Добавить новость в избранное: /api/news/favorites?api={api-key}&userId={string}&newsId={int}&actions=add
    - Удалить новость из избранного: /api/news/favorites?api={api-key}&userId={string}&newsId={int}&actions=del

#### Персональная лента новостей
- Запрос: /api/news/personal?api={api-key}&userId={string}
- Описание: Получить персонализированную ленту новостей для указанного пользователя.

## Формат ответа
Ответы API представлены в формате JSON и включают соответствующие коды состояния для успешных или ошибочных сценариев.

---

Если у вас есть вопросы или вам нужна дополнительная помощь, не стесняйтесь обращаться! 🚀

![alt text]([http://url/to/img.png](https://w7.pngwing.com/pngs/172/54/png-transparent-telegram-encapsulated-postscript-transfer-blue-angle-triangle.png)https://w7.pngwing.com/pngs/172/54/png-transparent-telegram-encapsulated-postscript-transfer-blue-angle-triangle.png)
