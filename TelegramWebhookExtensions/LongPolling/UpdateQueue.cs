using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot.Types;
using TelegramBotExtensions.Interfaces;

namespace TelegramBotExtensions.LongPolling
{
    public class UpdateQueue
    {
        private readonly ConcurrentQueue<Update> _updateQueue;
        private readonly AutoResetEvent _queueNotifier = new AutoResetEvent(false);
        public UpdateQueue()
        {
            _updateQueue = new ConcurrentQueue<Update>();
        }
        public Update Dequeue()
        {
            while (true)
            {
                _queueNotifier.WaitOne();
                if (!_updateQueue.TryDequeue(out var update))
                    continue;
                return update;
            }
        }

        public void Enqueue(Update objects)
        {
            _updateQueue.Enqueue(objects);
            _queueNotifier.Set();
        }
    }
}