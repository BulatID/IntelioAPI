using IntelioAPI;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Telegram.Bot.Types;

[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly NewsDbContext _dbContext;
    TelegramBot bot = new TelegramBot();
    public NewsController(NewsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void SendInfo()
    {
        bot.SendTextMessage($"Получен запрос от {HttpContext.Connection.RemoteIpAddress}");
    }

    [HttpGet("favorites")]
    public IActionResult Favorites(string userId, int newsId, string actions)
    {
        SendInfo();

        switch (actions)
        {
            case "get":

                List<Favorites> getFavorite = _dbContext.Favorites.Where(n => n.userId == userId).ToList();

                if (getFavorite == null)
                {
                    return NotFound("0");
                }
                else
                {
                    return Ok(getFavorite);
                }

            case "add":

                List<Favorites> foundFavorite = _dbContext.Favorites.Where(n => n.userId == userId && n.newsId == newsId).ToList();
                SendInfo();

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

    [HttpGet("rate")]
    public IActionResult Rate(string userId, int newsId, string actions)
    {
        SendInfo();

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
                SendInfo();

                if (foundRate.Count != 0)
                {
                    return NotFound("0");
                }

                Rates rate = new Rates
                {
                    userId = userId,
                    newsId = newsId
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
    public IActionResult GetNews()
    {
        /*List<News> newsList = _dbContext.News.ToList();
        SendInfo();
        return Ok(newsList);*/

        List<News> newsList = _dbContext.News.OrderByDescending(news => news.Date).ToList();
        SendInfo();
        return Ok(newsList);
    }

    [HttpGet("last")]
    public IActionResult GetLastNews(int count)
    {
        List<News> lastNewsList = _dbContext.News.OrderByDescending(n => n.Id).Take(count).ToList();
        SendInfo();
        return Ok(lastNewsList);
    }

    [HttpGet("search")]
    public IActionResult SearchNews(string contains)
    {
        List<News> foundNews = _dbContext.News.Where(n => n.Title.Contains(contains) || n.Content.Contains(contains)).ToList();
        SendInfo();

        if (foundNews.Count == 0)
        {
            return NotFound("Ничего не найдено!");
        }

        return Ok(foundNews);
    }

    [HttpGet("search/date")]
    public IActionResult SearchNewsByDate(string date)
    {
        SendInfo();

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
    public IActionResult GetUniqueCategories()
    {
        SendInfo();

        var uniqueCategories = _dbContext.News.Where(n => !string.IsNullOrEmpty(n.Category)).Select(n => n.Category).Distinct().ToList();

        if (uniqueCategories.Count == 0)
        {
            return NotFound("Категории не найдены!");
        }

        return Ok(uniqueCategories);
    }

    [HttpGet("category/search")]
    public IActionResult GetNewsByCategory(string selected)
    {
        SendInfo();

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
    public IActionResult GetNewsForToday()
    {
        try
        {
            DateTime today = DateTime.Today;

            if (_dbContext.News.Any())
            {
                List<News> newsListForToday = _dbContext.News.Where(news => news.Date.Date == today).ToList();

                if (newsListForToday.Any())
                {
                    SendInfo();
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
    public IActionResult GetNewsById(int newsId)
    {
        try
        {
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
    public string GetLastNewsId()
    {
        var lastId = _dbContext.News.OrderByDescending(n => n.Id).Select(n => n.Id).FirstOrDefault().ToString();
        SendInfo();

        return lastId;
    }
}