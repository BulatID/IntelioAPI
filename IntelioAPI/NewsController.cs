using IntelioAPI;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Telegram.Bot.Types;

[Route($"api/news")]
public class NewsController : ControllerBase
{
    private readonly NewsDbContext _dbContext;
    TelegramBot bot = new TelegramBot();
    public NewsController(NewsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool CheckAPI(string api)
    {
        var apikey = _dbContext.ApiKeys.FirstOrDefault(n => n.Key == api);

        if (apikey == null)
        {
            bot.SendTextMessage($"<b>Неудачная попытка запроса по ключу: неверный ключ</b>");
            return false;
        }

        bot.SendTextMessage($"<b>Получен запрос от</b> <code>{api}</code>\n\n" +
            $"📊 Информация о ключе:\n" +
            $"Пользователь: <a href=\"tg://user?id={apikey.ChatId}\">перейти</a>\n" +
            $"Баланс: <code>{apikey.Balance}</code>\n" +
            $"Всего сделано запросов: <code>{apikey.Count}</code>\n" +
            $"Дата генерации ключа: <code>{apikey.Created}</code>");

        if (apikey.Balance <= 0) return false;

        apikey.Balance = apikey.Balance - apikey.Tariff;
        apikey.Count = apikey.Count + 1;

        _dbContext.ApiKeys.Update(apikey);
        _dbContext.SaveChanges();

        return true;
    }

    [HttpGet("favorites")]
    public IActionResult Favorites(string api, string userId, int newsId, string actions)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        switch (actions)
        {
            case "get":

                List<Favorites> favoriteNews = _dbContext.Favorites.Where(n => n.userId == userId).ToList();

                if (favoriteNews == null || favoriteNews.Count == 0)
                {
                    return NotFound("0");
                }
                else
                {
                    List<int?> newsIds = favoriteNews.Select(f => f.newsId).ToList();

                    List<int> nonNullableNewsIds = newsIds.Where(id => id.HasValue).Select(id => id.Value).ToList();

                    List<News> userFavoriteNews = _dbContext.News.Where(n => nonNullableNewsIds.Contains(n.Id))
                                                                .OrderByDescending(n => n.Date)
                                                                .ToList();

                    return Ok(userFavoriteNews);
                }

            case "add":

                List<Favorites> foundFavorite = _dbContext.Favorites.Where(n => n.userId == userId && n.newsId == newsId).ToList();

                if (foundFavorite.Count != 0)
                {
                    return NotFound("0");
                }

                Favorites favorites = new Favorites
                {
                    userId = userId,
                    newsId = newsId
                };

                _dbContext.Favorites.AddAsync(favorites);
                _dbContext.SaveChangesAsync();

                return Ok("1");

            case "del":

                var deleteFav = _dbContext.Favorites.FirstOrDefault(n => n.userId == userId && n.newsId == newsId);

                if (deleteFav != null)
                {
                    _dbContext.Favorites.Remove(deleteFav);
                    _dbContext.SaveChanges();
                    return Ok("1");
                }
                else
                {
                    return NotFound("0");
                }

            default:

                return BadRequest("-1");
        }
    }

    [HttpGet("personal")]
    public IActionResult GetPersonalNews(string api, string userId)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        var userLikes = _dbContext.Rates.Where(r => r.userId == userId).Select(r => r.Category).ToList();

        var recommendedNews = new List<News>();

        if (userLikes.Any())
        {
            var categorizedNews = userLikes.SelectMany(category => _dbContext.News.Where(n => n.Category == category)).ToList();
            var rnd = new Random();
            recommendedNews = categorizedNews.OrderBy(x => rnd.Next()).ToList();
        }
        else
        {
            recommendedNews = _dbContext.News.OrderByDescending(n => n.Date).ToList();
        }

        return Ok(recommendedNews);
    }

    [HttpGet("rate")]
    public IActionResult Rate(string api, string userId, int newsId, string actions)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        switch (actions)
        {
            case "get":

                var getRate = _dbContext.Rates.FirstOrDefault(n => n.userId == userId && n.newsId == newsId);

                if (getRate == null)
                {
                    return NotFound("0");
                }
                else
                {
                    return Ok("1");
                }

            case "set":
                List<Rates> foundRate = _dbContext.Rates.Where(n => n.userId == userId && n.newsId == newsId).ToList();

                if (foundRate.Count != 0)
                {
                    return NotFound("0");
                }

                var news = _dbContext.News.FirstOrDefault(n => n.Id == newsId);

                Rates rate = new Rates
                {
                    userId = userId,
                    newsId = newsId,
                    Category = news.Category
                };

                _dbContext.Rates.AddAsync(rate);
                _dbContext.SaveChangesAsync();

                return Ok("1");

            case "del":

                var deleteRate = _dbContext.Rates.FirstOrDefault(n => n.userId == userId && n.newsId == newsId);

                if (deleteRate != null)
                {
                    _dbContext.Rates.Remove(deleteRate);
                    _dbContext.SaveChanges();
                    return Ok("1");
                } else
                {
                    return NotFound("0");
                }

            default:
                return BadRequest("-1");
        }
    }

    [HttpGet("all")]
    public IActionResult GetNews(string api)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        List<News> newsList = _dbContext.News.OrderByDescending(news => news.Date).ToList();
        return Ok(newsList);
    }

    [HttpGet("last")]
    public IActionResult GetLastNews(string api, int count)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        List<News> lastNewsList = _dbContext.News.OrderByDescending(n => n.Id).Take(count).ToList();
        return Ok(lastNewsList);
    }

    [HttpGet("search")]
    public IActionResult SearchNews(string api, string contains)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        List<News> foundNews = _dbContext.News.Where(n => n.Title.Contains(contains) || n.Content.Contains(contains)).ToList();

        if (foundNews.Count == 0)
        {
            return NotFound("Ничего не найдено!");
        }

        return Ok(foundNews);
    }

    [HttpGet("search/date")]
    public IActionResult SearchNewsByDate(string api, string date)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        if (!DateTime.TryParse(date, out DateTime searchDate))
        {
            return BadRequest("Неверный формат. Используйте формат yyyy-MM-dd");
        }

        DateTime searchDateStart = searchDate.Date;
        DateTime searchDateEnd = searchDateStart.AddDays(1);

        List<News> newsByDate = _dbContext.News.Where(n => n.Date >= searchDateStart && n.Date < searchDateEnd).ToList();

        if (newsByDate.Count == 0)
        {
            return NotFound("Ничего не найдено!");
        }

        return Ok(newsByDate);
    }

    [HttpGet("category")]
    public IActionResult GetUniqueCategories(string api)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        var uniqueCategories = _dbContext.News.Where(n => !string.IsNullOrEmpty(n.Category)).Select(n => n.Category).Distinct().ToList();

        if (uniqueCategories.Count == 0)
        {
            return NotFound("Категории не найдены!");
        }

        return Ok(uniqueCategories);
    }

    [HttpGet("category/search")]
    public IActionResult GetNewsByCategory(string api, string selected)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        var category = _dbContext.News
                            .Where(n => n.Category == selected)
                            .ToList();

        if (category.Count == 0)
        {
            return NotFound($"Нет новостей для жанра '{selected}'");
        }

        return Ok(category);
    }

    [HttpGet("today")]
    public IActionResult GetNewsForToday(string api)
    {
        try
        {
            if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

            DateTime today = DateTime.Today;

            if (_dbContext.News.Any())
            {
                List<News> newsListForToday = _dbContext.News.Where(news => news.Date.Date == today).ToList();

                if (newsListForToday.Any())
                {
                    return Ok(newsListForToday);
                }
                else
                {
                    return NotFound("Новости за сегодня не найдены.");
                }
            }
            else
            {
                return NotFound("Список новостей пуст.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Произошла ошибка!");
        }
    }

    [HttpGet("{newsId}")]
    public IActionResult GetNewsById(string api, int newsId)
    {
        try
        {
            if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

            var news = _dbContext.News.FirstOrDefault(news => news.Id == newsId);

            if (news != null)
            {
                return Ok(news);
            }
            else
            {
                return NotFound($"Новость с Id {newsId} не найдена.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Произошла ошибка при попытке получить новость по Id.");
        }
    }

    [HttpGet("latest")]
    public IActionResult GetLastNewsId(string api)
    {
        if (CheckAPI(api) == false) return BadRequest("Невозможно обработать запрос");

        var lastId = _dbContext.News.OrderByDescending(n => n.Id).Select(n => n.Id).FirstOrDefault().ToString();

        return Ok(lastId);
    }
}