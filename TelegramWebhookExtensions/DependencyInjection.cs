using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotExtensions.Interfaces;
using TelegramBotExtensions.LongPolling;
using TelegramWebhookExtensions.Middleware;

namespace TelegramBotExtensions
{
    public static class DependencyInjection
    {
        public static void AddTelegramBotClient(this IServiceCollection services)
        {
            services.AddTransient<ITelegramBotClient, TelegramBotClient>(m =>
            {
                var configuration = m.GetService<IConfiguration>();
                var telegramApiToken = configuration["TelegramSetting:TelegramApiToken"];
                return new TelegramBotClient(telegramApiToken);
            });
        }
        public static void AddTelegramBotClient(this IServiceCollection services,string telegramApiToken)
        {
            services.AddTransient<ITelegramBotClient, TelegramBotClient>(m =>
            {
                var configuration = m.GetService<IConfiguration>();
                return new TelegramBotClient(telegramApiToken);
            });
        }

        internal static int GetUpdatesLimit = 20;
        /// <summary>
        /// Add telegram LongPolling component
        /// </summary>
        /// <typeparam name="TUpdateHandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="getUpdatesLimit"><see href="https://core.telegram.org/bots/api#getupdates">getUpdates.limit</see></param>
        public static void AddLongPolling<TUpdateHandler>(this IServiceCollection services,int getUpdatesLimit = 20) 
            where TUpdateHandler : class,IUpdateHandler 
        {
            services.AddSingleton<UpdateQueue>();
            services.AddHostedService<LongPollingHostService>();
            services.AddScoped<IUpdateHandler, TUpdateHandler>();
            GetUpdatesLimit = getUpdatesLimit;
        }
        
        public static void AddTelegramWebhook<TUpdateHandler>(this IServiceCollection services)
            where TUpdateHandler : class, IUpdateHandler
        {
            services.AddScoped<IUpdateHandler, TUpdateHandler>();
        }

        public static IApplicationBuilder UseTelegramApiWebhookEndPoint(this IApplicationBuilder app)
        {
            app.UseTelegramWebhookMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("ok");
                    var updateHandler = context.RequestServices.GetService<IUpdateHandler>();
                    if (context.Features[typeof(Update)] is Update update)
                        await updateHandler.Process(update);
                });
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hi,Telegram Bot Webhook is Starting.");
                });
            });
            return app;
        }
        /// <summary>
        /// if <param name="webhookUrl"></param> is null, webhookUrl will use configuration(TelegramSetting:TelegramApiWebhookUrl)
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="webhookUrl"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">TelegramSetting:TelegramApiWebhookUrl configuration didn't set</exception>
        public static async Task SetWebhookInfo(this IServiceProvider serviceProvider,string webhookUrl = null)
        {
            var configure = serviceProvider.CreateScope().ServiceProvider.GetService<IConfiguration>();
            var telegramBot = serviceProvider.CreateScope().ServiceProvider.GetService<ITelegramBotClient>();
            await telegramBot.TestApiAsync();
            webhookUrl ??= configure["TelegramSetting:TelegramApiWebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));
            await telegramBot.SetWebhookAsync(webhookUrl);
        }
    }
}