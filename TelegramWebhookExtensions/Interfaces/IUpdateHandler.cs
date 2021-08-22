using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBotExtensions.Interfaces
{
    public interface IUpdateHandler
    {
        Task Process(Update update);
    }
}