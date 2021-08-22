using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotExtensions.Interfaces;
using static System.Threading.Tasks.Task;

namespace TelegramBotExtensions.LongPolling
{
    public class LongPollingHostService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UpdateQueue _updateQueue;
        private readonly ILogger<LongPollingHostService> _logger;
        private readonly int _getUpdatesLimit;

        public LongPollingHostService(IServiceProvider serviceProvider, UpdateQueue updateQueue, ILogger<LongPollingHostService> logger)
        {
            _serviceProvider = serviceProvider;
            _updateQueue = updateQueue;
            _logger = logger;
            _getUpdatesLimit = DependencyInjection.GetUpdatesLimit;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Thread1 : send getUpdateInfo request to telegram,and enqueue Update message to  UpdateMessageQueue
            Run(() => UpdateInfoMonitor(stoppingToken), stoppingToken);
            // Thread2 :Dequeue UpdateMessageQueue to get Update message and process
            Run(() => RequestProcessorController(stoppingToken), stoppingToken);
            return CompletedTask;
        }

        public override Task StartAsync(CancellationToken token = default)
        {
            var clearWebhookInfoTask = ClearWebhookInfo(token);
            WaitAll(clearWebhookInfoTask);
            return base.StartAsync(token);
        }

        /// <summary>
        /// UpdateInfoMonitor will send getUpdateInfo Request to get new <see cref="Update"/> object,which be enqueue to <see cref="UpdateQueue"/>
        /// </summary>
        private Task UpdateInfoMonitor(CancellationToken cancellationToken)
        {
            var currentOffset = 0;
            _logger.LogInformation("Start UpdateInfoMonitor");
            while (true)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var task = SendGetUpdateRequestAsync(currentOffset, cancellationToken);
                    task.Wait(cancellationToken);
                    var updates = task.Result;
                    foreach (var update in updates)
                    {
                        _updateQueue.Enqueue(update);
                        currentOffset = update.Id;
                    }

                    if (updates.Any())
                        currentOffset++;
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e.ToString());
                    _logger.LogInformation("ReStart UpdateInfoMonitor");
                }
            }

            return FromCanceled(cancellationToken);
        }
        private async Task ClearWebhookInfo(CancellationToken token)
        {
            var telegramBot = _serviceProvider.CreateScope().ServiceProvider.GetService<ITelegramBotClient>();
            await telegramBot.TestApiAsync(token);
            await telegramBot.SetWebhookAsync("", cancellationToken: token);
        }

        private async Task<Update[]> SendGetUpdateRequestAsync(int offset, CancellationToken token)
        {
            try
            {
                var telegramBotClient =
                    _serviceProvider.CreateAsyncScope().ServiceProvider.GetService<ITelegramBotClient>();
                //_logger.LogInformation($"Client is sending GetUpdates(offset:{offset}) request to Telegram Api...");
                var updates = await telegramBotClient.GetUpdatesAsync(offset,
                    _getUpdatesLimit, cancellationToken: token);
                //_logger.LogInformation($"GetUpdates(offset:{offset}) request is success!, updates count is {updates.Length}");
                return updates;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.ToString());
                return new Update[0];
            }
        }

        private void RequestProcessorController(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var update = _updateQueue.Dequeue();
                Run(async () => await CreateUpdateHandlerProcessTask(update), token);
            }
        }
        private async Task CreateUpdateHandlerProcessTask(Update update)
        {
            await Run(async () =>
            {
                var scope = _serviceProvider.CreateScope().ServiceProvider;
                var updateHandler = scope.GetService<IUpdateHandler>();
                await updateHandler.Process(update);
            });
        }
    }
}