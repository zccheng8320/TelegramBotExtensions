using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types;


namespace TelegramWebhookExtensions.Middleware
{
    /// <summary>
    /// Use TelegramWebhookMiddleware,this middleware will deserialize request body to <see cref="Update"/> object which set to <see cref="HttpContext.Features"/>
    /// </summary>
    public static class MiddleWareExtensions
    {
        public static IApplicationBuilder UseTelegramWebhookMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TelegramWebhookMiddleware>();
        }
    }
    internal class TelegramWebhookMiddleware
    {
        private readonly ILogger<TelegramWebhookMiddleware> _logger;
        private readonly RequestDelegate _next;

        public TelegramWebhookMiddleware(ILogger<TelegramWebhookMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var update = await ConvertAsync(context.Request);
                context.Features.Set(update);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.ToString());
            }
            await _next.Invoke(context);
        }
        private async Task<Update> ConvertAsync(HttpRequest request)
        {
            try
            {
                var jsonString = await GetJsonString(request);
                var update = JsonConvert.DeserializeObject<Update>(jsonString);
                return await new ValueTask<Update>(update);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.ToString());
                return await Task.FromException<Update>(e);
            }
        }
        private async Task<string> GetJsonString(HttpRequest request)
        {
            var streamReader = new StreamReader(request.Body);
            return await new ValueTask<string>(streamReader.ReadToEndAsync());
        }

    }
}