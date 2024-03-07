using IntelioAPI;
using Microsoft.AspNetCore.Html;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.EntityFrameworkCore;

public class NewsService
{
    private readonly NewsDbContext _dbContext;
    WebClient client = new WebClient();
    TelegramBot bot = new TelegramBot();
    public NewsService(NewsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateNewsPeriodically()
    {
        Timer timer = null;

        timer = new Timer(async (e) =>
        {
            List<RssSource> rssSources;
            string[] banWord;
            using (var context = new NewsDbContext())
            {
                rssSources = context.RssSources.ToList();
                banWord = context.StopWords.Select(stopWord => stopWord.word).ToArray();
            }

            GetPicture pic = new GetPicture();

            bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Приступаю к сканированию сайтов");

            foreach (var source in rssSources)
            {
                try
                {
                    int i = 0;
                    using (var client = new WebClient())
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        client.Encoding = Encoding.GetEncoding("windows-1251");
                        byte[] data = client.DownloadData(source.Url);
                        string xmlData = Encoding.GetEncoding("windows-1251").GetString(data);

                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.DtdProcessing = DtdProcessing.Ignore;

                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlData)))
                        {
                            using (XmlReader reader = XmlReader.Create(ms, settings))
                            {
                                SyndicationFeed feed = SyndicationFeed.Load(reader);
                                bool boolBan = false;
                                foreach (var item in feed.Items)
                                {
                                    string content = Regex.Replace(Regex.Replace(Regex.Replace(Encoding.UTF8.GetString(Encoding.GetEncoding("windows-1251").GetBytes(item.Summary.Text)), @"<.*?>", string.Empty), @"&[\w#]+?;", m => { switch (m.Value) { case "&quot;": return "\""; case "&nbsp;": return " "; default: return string.Empty; } }), @"\s+", " ").Trim();

                                    foreach (var word in banWord)
                                    {
                                        if (content.Contains(word))
                                        {
                                            boolBan = true;
                                            bot.SendTextMessage($"🤫 Цензурирую в {source.Url} слово: <code>{word}</code>");
                                        }
                                    }

                                    if (boolBan)
                                    {
                                        boolBan = true;
                                        break;
                                    }

                                    var news = new News
                                    {
                                        Title = Encoding.UTF8.GetString(Encoding.GetEncoding("windows-1251").GetBytes(item.Title.Text)),
                                        Content = content,
                                        Date = item.PublishDate.DateTime,
                                        Source = "",
                                        ImageUrl = "",
                                        Category = ""
                                    };

                                    foreach (SyndicationLink link in item.Links)
                                    {
                                        if (link.RelationshipType == "alternate")
                                        {
                                            news.Source = link.Uri.ToString();
                                            break;
                                        }
                                    }

                                    foreach (SyndicationCategory category in item.Categories)
                                    {
                                        news.Category = Encoding.UTF8.GetString(Encoding.GetEncoding("windows-1251").GetBytes(category.Name));

                                        if (news.Category == null || news.Category == "")
                                            news.Category = "Без категории";
                                        
                                        break;
                                    }

                                    using (var db = new NewsDbContext())
                                    {
                                        if (!db.News.Any(n => n.Title == news.Title))
                                        {
                                            string picture = pic.getPictureUrl(news.Source);
                                            
                                            if (picture != null)
                                            {
                                                news.ImageUrl = picture;
                                                await Task.Delay(3000);
                                            }

                                            if (i == 0)
                                            {
                                                Console.WriteLine($"{DateTime.Now}: Сканирую сайт {source.Url}");
                                                //bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Сканирую сайт {source.Url}");
                                            }
                                            i++;

                                            await db.News.AddAsync(news);
                                        }
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}]: При сканировании сайта произошла ошибка!");
                }
            }
            bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Сканирование сайтов окончено");
            deleteTrash();
            timer.Change(1800000, Timeout.Infinite);
        }, null, 0, Timeout.Infinite);
    }

    public void deleteTrash()
    {
        string searchPattern = "core.*";
        string directoryPath = AppDomain.CurrentDomain.BaseDirectory;

        string[] files = Directory.GetFiles(directoryPath, searchPattern);

        foreach (string file in files)
        {
            File.Delete(file);
            Console.WriteLine($"Файл '{file}' был удален!.");
        }
    }

    public string GetParameter(string name)
    {
        var parameterValue = _dbContext.Parameters
            .FirstOrDefault(p => p.Name == name)?.Value;

        if (string.IsNullOrEmpty(parameterValue))
        {
            return "Empty";
        }
        else
        {
            return parameterValue;
        }
    }
}

