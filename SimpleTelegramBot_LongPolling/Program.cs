using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SimpleTelegramBot_LongPolling;
using TelegramBotExtensions;

namespace TelegramBotLongPollingSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }
        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    // add telegram bot client
                    services.AddTelegramBotClient();
                    // register your update handeler
                    services.AddLongPolling<SampleUpdateHandler>(50);
                });
        }
    }
}