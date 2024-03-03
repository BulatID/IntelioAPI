using IntelioAPI;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

[Route("api/getpay")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly NewsDbContext _dbContext;
    WebClient client = new WebClient();
    TelegramBot bot = new TelegramBot();
    public WebhookController(NewsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public IActionResult Post([FromForm] WebhookData data)
    {
        bot.SendTextMessage("<b>Пополнение счёта!</b>\n\n" +
            $"merchant_id={data.merchant_id}, invoice_id={data.invoice_id}, order_id={data.order_id}, amount={data.amount}");

        var pay = _dbContext.PayList.FirstOrDefault(n => n.id == Convert.ToInt32(data.order_id));
        var balance = _dbContext.ApiKeys.FirstOrDefault(n => n.ChatId == pay.ChatId);

        if (pay != null && balance != null)
        {
            balance.Balance = data.amount;
        }
        else return BadRequest();

        return Ok();
    }
}

public class WebhookData
{
    public Guid merchant_id { get; set; }
    public Guid invoice_id { get; set; }
    public string order_id { get; set; }
    public float amount { get; set; }
}
