using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly NewsDbContext _dbContext;

    public NewsController(NewsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("all")]
    public IActionResult GetNews()
    {
        List<News> newsList = _dbContext.News.ToList();
        return Ok(newsList);
    }

    [HttpGet("last")]
    public IActionResult GetLastNews(int count)
    {
        List<News> lastNewsList = _dbContext.News.OrderByDescending(n => n.Id).Take(count).ToList();
        return Ok(lastNewsList);
    }

    [HttpGet("search")]
    public IActionResult SearchNews(string contains)
    {
        List<News> foundNews = _dbContext.News.Where(n => n.Title.Contains(contains) || n.Content.Contains(contains)).ToList();

        if (foundNews.Count == 0)
        {
            return NotFound("Ничего не найдено!");
        }

        return Ok(foundNews);
    }

    [HttpGet("search/date")]
    public IActionResult SearchNewsByDate(string date)
    {
        if (!DateTime.TryParse(date, out DateTime searchDate))
        {
            return BadRequest("Invalid date format. Please provide the date in yyyy-MM-dd format.");
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
}