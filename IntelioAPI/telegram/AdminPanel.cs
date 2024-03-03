using Deployf.Botf;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace IntelioAPI
{
    public class AdminPanel : BotfProgram
    {
        private readonly NewsDbContext _dbContext;
        public AdminPanel(NewsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Action("🛠 Админ-панель")]
        public async Task Panel()
        {
            if (await isAdmin() == false) return;

            Reply();
            PushLL("<b>🛠 Админ-панель</b>");
            Push("⚠️ Будьте осторожны ⚠️");
            RowKButton("🗂 Управление источниками");
            RowKButton("🚫 Управление стоп-словами");
            RowKButton("📥 Скачать базу данных");
            await Send();
            //RowKButton("Статистика");
            //RowKButton("Провести рассылку внутри бота");
        }

        [Action("🚫 Управление стоп-словами")]
        public async Task stopwords()
        {
            if (await isAdmin() == false) return;

            PushL("<b>Выберите нужный пункт:</b>");
            Button("➕ Добавить новое стоп-слово", Q(AddStopWord));
            Button("➖ Удалить стоп-слово", Q(DelStopWord));
        }

        [Action]
        public async Task AddStopWord()
        {
            if (await isAdmin() == false) return;

            PushLL("<b>Введите новое слово или словосочетание</b>");
            PushL("Введите /stop для отмены");
            await Send();

            var response = await AwaitText();

            if (response == "/stop") return;

            var existing = await _dbContext.StopWords.FirstOrDefaultAsync(current => current.word == response);

            if (existing != null)
            {
                await Send("<b>Данное слово/словосочетание уже существует в базе данных</b>");
                return;
            }

            StopWords word = new StopWords
            {
                word = response
            };

            await _dbContext.StopWords.AddAsync(word);
            await _dbContext.SaveChangesAsync();

            Button("➕ Добавить новое стоп-слово", Q(AddStopWord));
            Button("➖ Удалить стоп-слово", Q(DelStopWord));
            await Send("<b>Успешно добавлено</b>");
        }

        [Action]
        public async Task DelStopWord()
        {
            if (await isAdmin() == false) return;

            PushLL("<b>Выберите слово или словосочетание, которое нужно удалить</b>");
            PushL("Введите /stop для отмены");
            await Send();

            List<StopWords> lastWordList = _dbContext.StopWords.OrderByDescending(n => n.word).ToList();

            foreach (var Word in lastWordList)
            {
                RowKButton(Word.word.ToString());
            }

            await Send();

            var response = await AwaitText();

            if (response == "/stop")
                return;

            StopWords stopw = _dbContext.StopWords.FirstOrDefault(n => n.word == response);
            if (stopw != null)
            {
                _dbContext.StopWords.Remove(stopw);
                _dbContext.SaveChanges();

                Button("➕ Добавить стоп-слово", Q(AddStopWord));
                Button("➖ Удалить еще один", Q(DelStopWord));
                await Send("<b>Успешно удалили</b>");
            }
            else
            {
                await Send("<b>Не удалось удалить!</b>");
            }
        }

        [Action("📥 Скачать базу данных")]
        public async Task DBdownload()
        {
            if (await isAdmin() == false)
                return;

            using (var stream = System.IO.File.Open("data.db", System.IO.FileMode.Open))
            {
                InputOnlineFile inputFile = new InputOnlineFile(stream, "data.db");

                var message = await Context.Bot.Client.SendDocumentAsync(
                    chatId: ChatId,
                    document: inputFile,
                    caption: $"<b>Версия на </b><code>{DateTime.Now}</code> <b>для</b> <code>{ChatId}</code>",
                    parseMode: ParseMode.Html
                );
            }
        }

        [Action("🗂 Управление источниками")]
        public async Task ContentControl()
        {
            if (await isAdmin() == false)
                return;

            PushL("<b>Выберите нужный пункт:</b>");
            Button("➕ Добавить RSS-источник", Q(addRss));
            Button("➖ Удалить RSS-источник", Q(delRss));
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

            Button("➕ Добавить еще один", Q(addRss));
            Button("➖ Удалить", Q(delRss));
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

                Button("➕ Добавить RSS-источник", Q(addRss));
                Button("➖ Удалить еще один", Q(delRss));
                await Send("<b>Успешно удалили</b>");
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
