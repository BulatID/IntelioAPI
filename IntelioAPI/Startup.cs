using Deployf.Botf;
using IntelioAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped<NewsService>();
        services.AddScoped<AdminPanel>();
        services.AddDbContext<NewsDbContext>();
        services.AddBotf("Telegram key");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, NewsService newsService)
    {
        app.UseRouting();
        app.UseBotf();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });


        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        using (var scope = app.ApplicationServices.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
            dbContext.EnsureDbCreated();
        }
        newsService.UpdateNewsPeriodically();
    }

}
