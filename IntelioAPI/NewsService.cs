using IntelioAPI;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

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

        timer = new Timer((e) =>
        {
            try
            {

                List<RssSource> rssSources;

                using (var context = new NewsDbContext())
                {
                    rssSources = context.RssSources.ToList();
                }

                GetPicture pic = new GetPicture();

                bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Приступаю к сканированию сайтов");

                foreach (var source in rssSources)
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

                                foreach (var item in feed.Items)
                                {
                                    var news = new News
                                    {
                                        Title = Encoding.UTF8.GetString(Encoding.GetEncoding("windows-1251").GetBytes(item.Title.Text)),
                                        Content = Encoding.UTF8.GetString(Encoding.GetEncoding("windows-1251").GetBytes(item.Summary.Text)),
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
                                                Task.Delay(3000);
                                            }

                                            if (i == 0)
                                            {
                                                Console.WriteLine($"{DateTime.Now}: Сканирую сайт {source.Url}");
                                                //bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Сканирую сайт {source.Url}");
                                            }
                                            i++;

                                            db.News.Add(news);
                                        }
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }
                bot.SendTextMessage($"[<code>{DateTime.Now}</code>]: Сканирование сайтов окончено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[<code>{DateTime.Now}</code>]: При сканировании сайта произошла ошибка!");
            }
            timer.Change(1800000, Timeout.Infinite);
        }, null, 0, Timeout.Infinite);
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

