using Deployf.Botf;
using IntelioAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Telegram.Bot.Types;

public class Program : BotfProgram
{

    public static async Task Main(string[] args)
    {

        var webHost = CreateHostBuilder(args).Build();

        await webHost.RunAsync(new System.Threading.CancellationTokenSource().Token);

        await Task.Delay(-1);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                //webBuilder.UseUrls("http://localhost:4001");
            });
}
