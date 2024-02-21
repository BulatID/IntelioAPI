# IntelioAPI

#GET-запрос:
###🔹/api/news/all — получает все новости, которые есть в базе
###🔹/api/news/last?count={int} — необходимо будет указать количество новостей, которое нужно вернуть.
###🔹/api/news/search?contains={string} — необходимо будет указать текст, которое нужно вернуть.
###🔹/api/news/search/date?date={string} — необходимо будет указать текст в виде даты в формате "yyyy-MM-dd" (Год, месяц, день). Пример: /api/news/search/date?date=2024-02-14
