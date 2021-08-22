using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotExtensions.Interfaces;

namespace SimpleTelegramBot_Webhook
{
    public class SampleUpdateHandler :IUpdateHandler
    {
        private readonly ITelegramBotClient _client;

        public SampleUpdateHandler(ITelegramBotClient client)
        {
            _client = client;
        }
        public async Task Process(Update update)
        {
            var chatId = update.Message.Chat.Id;
            var userName = update.Message.From.FirstName + update.Message.From.LastName;
            var text = update.Message.Text;
            await _client.SendTextMessageAsync(chatId, $"Hello,{userName}.You said {text}");
        }
    }
}