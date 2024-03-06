using Deployf.Botf;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using System.Text;
using System.Web;
using XSystem.Security.Cryptography;

namespace IntelioAPI.telegram
{
    public class UserPanel : BotController
    {
        private readonly NewsDbContext _dbContext;
        public UserPanel(NewsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Action("/start", "🚀 Запустить / перезагрузить бота")]
        public async Task Start()
        {
            PushLL($"<b>Добро пожаловать,</b> <code>{Context!.GetUserFullName()!.Trim()} 👋</code>");
            PushL("Выберите нужный пункт:");
            RowButton("🧑‍💻 Для разработчиков", Q(developerMenu));

            if (await isAdmin() == true)
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

        [Action("/start")]
        public async Task StartByRefferal(string refferal)
        {
            if (refferal == "failed_pay")
            {
                await Context.Bot.Client.SendStickerAsync(ChatId, "CAACAgIAAxkBAAEB6CFlTlwkKuWtm9D5jvv-3FjI-g1fEwACDgADwDZPEyNXFESHbtZlMwQ");
                Button("🧑‍💻 Контакт поддержки", $"https://t.me/BulatID");
                await Send("<b>Оплата не удалась! Попробуйте снова</b>");
                return;
            }

            if (refferal == "successful_pay")
            {
                await Context.Bot.Client.SendStickerAsync(ChatId, "CAACAgIAAxkBAAEB6BllTlvlAdpijt_6rQABftdZ22PxbOwAAhkAA8A2TxPQQ4D2IFcUSzME");
                Button("🧑‍💻 Контакт поддержки", $"https://t.me/BulatID");
                await Send("<b>Спасибо за покупку!</b>");
                return;
            }
            await Start();
        }

        [Action("/dev", "💻 Для разработчиков")]
        private async Task developerMenu()
        {
            var apikey = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == ChatId);
            
            Photo("https://i.ibb.co/G0fRfTJ/info.png");
            PushLL("<b>💻 Панель разработчика</b>");
            
            if (apikey != null)
            {
                PushL($"<b>🔑 Ваш ключ:</b> <code>{apikey.Key}</code>");
                PushL($"<b>Ваш тариф за один запрос:</b> <code>{apikey.Tariff} ₽</code>");
                RowButton("ℹ️ Информация о счете", Q(payMenu));
                RowButton("🗑 Удалить API-ключ", Q(deleteKey));
            }
            else
            {
                PushLL("Intelio предлагает вам интегрировать получение контента в ваши сайты и приложения. Такой подход позволит вам максимально гибко адаптировать возможности нашей системы для ваших нужд.");
                PushLL("Базовая стоимость одного запроса составляет <code>0.03 ₽</code>");
                PushLL("<i>Если вы отправляете (или планируете отправлять) тысячи и десятки тысяч запросов к нашему API каждый месяц, мы готовы предоставить скидку.</i>");

                RowButton("🚀 Выпустить API-ключ", Q(addKey));
            }
            RowButton("◀️ Назад", Q(Start));
        }

        [Action]
        public async Task payMenu()
        {
            var apikey = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == ChatId);

            PushLL("<b>ℹ️ Информация о счете</b>");
            PushL($"Ваш баланс составляет: <code>{apikey.Balance} ₽</code>");

            RowButton("🛒 Пополнить счёт", Q(ReadMoneyCount));
            RowButton("◀️ Назад", Q(developerMenu));
        }

        [Action]
        public async Task ReadMoneyCount()
        {
            var delete = await Send("<b>Введите сумму для пополнения:</b>\n\nНапишите /no для отмены ввода");

            var summa = await AwaitText();

            float price = 0;

            if (summa == "/no") return;

            if(float.TryParse(summa, out price))
            {
                if(price <= 0)
                {
                    await Send("<b>Введена неверная сумма!</b>");
                    return;
                }

                PayList pay = new PayList
                {
                    ChatId = ChatId,
                    Balance = price,
                    Created = DateTime.Now
                };

                await _dbContext.PayList.AddAsync(pay);
                await _dbContext.SaveChangesAsync();

                var id = _dbContext.PayList.FirstOrDefault(n => n.ChatId == ChatId && n.Balance == price);

                if (id == null) return;

                await Context.Bot.Client.DeleteMessageAsync(ChatId, delete.MessageId);

                string url = createBill(id.id, price);

                PushLL("<b>📝 Счёт на оплату успешно выставлен!</b>");
                PushLL($"<b>Сумма:</b> <code>{price} ₽</code>");
                PushL($"<b>❗️ Оплата проверяется автоматически (Идентификатор заказа:</b> <code>{id.id}</code><b>)</b>");
                RowButton(WebApp("💳 Оплатить", url));
                RowButton("◀️ Назад", Q(payMenu));
                Button("🧑‍💻 Контакт поддержки", $"https://t.me/BulatID");
            } else
            {
                await Send("<b>Некорректное значение!</b>");
            }

        }

        [Action("/balance")]
        public async Task setBalance(long cId, double amount)
        {
            if(await isAdmin() == false) return;

            var id = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == cId);

            if (id == null)
            {
                await Send("<b>Не удалось изменить баланс</b>");
                return;
            }

            id.Balance = amount;

            _dbContext.ApiKeys.Update(id);
            await _dbContext.SaveChangesAsync();

            await Send("<b>Баланс успешно изменен!</b>");
        }

        [Action("/getapi")]
        public async Task IntelioStaticApiKey()
        {
            if (await isAdmin() == false) return;

            var id = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == 0000);

            if (id == null)
            {
                ApiKeys api = new ApiKeys
                {
                    ChatId = 0000,
                    Count = 0,
                    Key = "0203infinity2024",
                    Balance = 99999999
                };
                
                await _dbContext.ApiKeys.AddAsync(api);
                await _dbContext.SaveChangesAsync();

                await Send($"<b>Создано!\n\nПостоянный api-ключ для приложения Intelio:</b> <code>{api.Key}</code>");
            } else
            {
                await Send($"<b>Постоянный api-ключ для приложения Intelio:</b> <code>{id.Key}</code>");
            }
        }

        [Action]
        public async Task addKey()
        {
            var check = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == ChatId);

            if (check == null)
            {
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890123456789";
                StringBuilder result = new StringBuilder();

                for (int i = 0; i < 8; i++)
                {
                    result.Append(chars[random.Next(chars.Length)]);
                }

                string randomString = result.ToString();

                ApiKeys api = new ApiKeys
                {
                    ChatId = ChatId,
                    Balance = 10,
                    Count = 0,
                    Tariff = 0.03,
                    Key = randomString,
                    Created = DateTime.Now
                };

                await _dbContext.ApiKeys.AddAsync(api);
                await _dbContext.SaveChangesAsync();

                await developerMenu();

            } else
            {
                PushLL("<b>Произошла ошибка при создании ключа!</b>");
                PushL("Пожалуйста, свяжитесь с администратором для решения проблемы");
                RowButton("🧑‍💻 Для разработчиков", Q(developerMenu));
                RowButton("👤 Контакт Администратора", "https://t.me/BulatID");
            }
        }

        [Action]
        public async Task deleteKey()
        {
            var delete = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == ChatId);

            if (delete != null)
            {
                _dbContext.ApiKeys.Remove(delete);
                _dbContext.SaveChanges();

                await developerMenu();
            }
            else
            {
                PushLL("<b>Произошла ошибка при удалении ключа!</b>");
                PushL("Пожалуйста, свяжитесь с администратором для решения проблемы");
                RowButton("🧑‍💻 Для разработчиков", Q(developerMenu));
                RowButton("👤 Контакт Администратора", "https://t.me/BulatID");
            }
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

        [On(Handle.Exception)]
        public void ExceptionGeneralHandler(Exception e)
        {
            Context.Bot.Client.SendTextMessageAsync(-1002144477508, $"🛑 Произошла ошибка, клиент оповещен об ошибке.\n\nСработало исключение:\n{e}");

            Reply();
            Photo("https://i.ibb.co/0MD82dG/error-pic.png");
            PushLL("<b>Произошла неизвестная ошибка!</b>");
            PushL("Сообщение разработчикам уже отправлено.");
        }

        [Action]
        private string createBill(int orderId, float amount)
        {
            string merchant_id = "8758fcd6-cc19-4df6-bdce-f91f6a66f849",
            currency = "RUB",
            secret = "fe9349e423fa34353fcd9207205b4a19",
            order_id = orderId.ToString(),
            desc = $"Пополнение счёта для {ChatId} на сумму {amount} руб",
            lang = "ru",

            sign = string.Join(":", merchant_id, amount, currency, secret, order_id);

            var sha256 = new SHA256Managed();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sign));
            string sign_hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["merchant_id"] = merchant_id;
            parameters["amount"] = amount.ToString();
            parameters["currency"] = currency;
            parameters["order_id"] = order_id;
            parameters["sign"] = sign_hash;
            parameters["desc"] = desc;
            parameters["lang"] = lang;

            string url = "https://aaio.so/merchant/pay?" + parameters.ToString();

           return url;
        }
    }
}
