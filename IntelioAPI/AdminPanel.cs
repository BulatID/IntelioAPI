using Deployf.Botf;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;

namespace IntelioAPI
{
    public class AdminPanel : BotfProgram
    {
        private readonly NewsDbContext _dbContext;
        public AdminPanel(NewsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Action("/start", "🚀 Запустить / перезагрузить бота")]
        public async Task Start()
        {
            int lastId = Convert.ToInt32(_dbContext.News.OrderByDescending(n => n.Id).Select(n => n.Id).FirstOrDefault().ToString());
            
            PushLL($"<b>Добро пожаловать,</b> <code>{Context!.GetUserFullName()!.Trim()} 👋</code>");
            PushL("🔽 Выберите нужную кнопку на клавиатуре 🔽");
            RowButton("📰 Читать все новости", Q(ReadNews, lastId));
            await Send();
            if (await isAdmin())
            {
                RowKButton("🛠 Админ-панель");
            }

            var existingUser = _dbContext.TGuser.FirstOrDefault(currentUser => currentUser.id == ChatId);

            if (existingUser == null)
            {
                DateTime currentTime = DateTime.Now;
                string formattedTime = currentTime.ToString("yyyy-MM-dd HH:mm:ss");

                TGUser user = new TGUser
                {
                    id = ChatId,
                    username = Context!.GetUsername()!.Trim(),
                    name = Context!.GetUserFullName()!.Trim(),
                    jointime = Convert.ToDateTime(formattedTime)
                };

                await _dbContext.TGuser.AddAsync(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        [Action]
        public async void ReadNews(int sId)
        {
            try
            {
                var selectedNews = _dbContext.News.FirstOrDefault(newsDB => newsDB.Id == sId);
                Photo(selectedNews.ImageUrl.ToString());
                PushLL($"<b>{selectedNews.Title}</b>");
                PushLL(selectedNews.Content.ToString());
                PushL($"Дата: <code>{selectedNews.Date}</code> | id: <code>{selectedNews.Id}</code>");
                RowButton("🔗 Открыть источник", $"{selectedNews.Source}");
            }
            catch
            {
                PushL("Не удалось загрузить новость");
            }

            int next = sId + 1;
            int prev = sId - 1;

            if (CheckIfIdExists(next))
                RowButton("⬅️", Q(ReadNews, next));

            if(CheckIfIdExists(prev))
                Button("➡️", Q(ReadNews, prev));
        }

        [Action]
        public bool CheckIfIdExists(int id)
        {
            return _dbContext.News.Any(n => n.Id == id);
        }

        [Action("🛠 Админ-панель")]
        public async Task Panel()
        {
            if (await isAdmin() == false)
                return;

            PushLL("<b>🛠 Админ-панель</b>");
            Push("⚠️ Будьте осторожны ⚠️");
            RowKButton("Управление источниками");
            //RowKButton("Управление стоп-словами");
            //RowKButton("Статистика");
            //RowKButton("Провести рассылку внутри бота");
        }

        [Action("Управление источниками")]
        public async Task ContentControl()
        {
            if (await isAdmin() == false)
                return;

            PushL("<b>Выберите нужный пункт:</b>");
            Button("Добавить RSS-источник", Q(addRss));
            Button("Удалить RSS-источник", Q(delRss));
        }

        [Action]
        public async Task addRss()
        {
            if (await isAdmin() == false)
                return;

            PushLL("<b>Введите ссылку на источник</b>");
            PushLL("Формат: <code>https://www.example.com</code>");
            PushL("Введите /stop для отмены");
            await Send();

            var response = await AwaitText();

            if (response == "/stop")
                return;

            var existing = await _dbContext.RssSources.FirstOrDefaultAsync(current => current.Url == response);
            
            if(existing != null)
            {
                await Send("<b>Такой источник уже находится в базе!</b>");
                return;
            }

            RssSource rss = new RssSource
            {
                Url = response
            };

            await _dbContext.RssSources.AddAsync(rss);
            await _dbContext.SaveChangesAsync();

            await Send("<b>Источник добавлен!</b>");
        }

        [Action]
        public async Task delRss()
        {
            if (await isAdmin() == false)
                return;

            PushLL("<b>Нажмите на нужный url, чтобы удалить из БД</b>");
            PushL("Введите /stop для отмены");

            List<RssSource> lastUrlList = _dbContext.RssSources.OrderByDescending(n => n.Url).ToList();

            foreach (var url in lastUrlList)
            {
                RowKButton(url.Url.ToString());
            }

            await Send();

            var response = await AwaitText();

            if (response == "/stop")
                return;

            RssSource source = _dbContext.RssSources.FirstOrDefault(n => n.Url == response);
            if (source != null)
            {
                _dbContext.RssSources.Remove(source);
                _dbContext.SaveChanges();
            } else
            {
                await Send("<b>Не удалось удалить!</b>");
            }
        }

        [Action("/RSS")]
        public async Task ScanServicesControl(string status)
        {
            if (await isAdmin() == false)
                return;

            if (status == "true" || status == "false")
                SetParameter("RSS", status);
            else
                await Send("Введен неверный параметр");
        }

        [Action]
        public string GetParameter(string name)
        {
            var parameterValue = _dbContext.Parameters
                .FirstOrDefault(p => p.Name == name)?.Value;

            if (string.IsNullOrEmpty(parameterValue))
            {
                return "Empty";
            } else
            {
                return parameterValue;
            }
        }

        [Action]
        public async void SetParameter(string name, string value)
        {
            var existingParameter = _dbContext.Parameters.FirstOrDefault(p => p.Name == name);

            if (existingParameter != null)
            {
                existingParameter.Value = value;
            }
            else
            {
                var newParameter = new Parameters { Name = name, Value = value };
                _dbContext.Parameters.Add(newParameter);
            }
            _dbContext.SaveChanges();
            await Send("Параметр применен!");
        }

        [Action]
        private async Task<bool> isAdmin()
        {
            var chatMember = await Context.Bot.Client.GetChatMemberAsync(-1002029015444, ChatId);

            if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator)
                return true;
            else
                return false;
        }
    }
}
