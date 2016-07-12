using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace soa
{
	public interface IMessageWriter
	{
		void write(Message message);
	}

    public class MessageWriter : ThreadWorker, IMessageWriter
    {
        private const int MSG_QUEUE_MAX_SIZE = 1024;
        private readonly Queue<Message> messages;
        private readonly INetwork network;

        public MessageWriter(INetwork network)
        {
            this.network = network;
            this.messages = new Queue<Message>(MSG_QUEUE_MAX_SIZE);
        }

        public override void Stop()
        {
            lock(messages)
            {
                alive = false;
                Monitor.PulseAll(messages);
            }
            base.Stop();
        }

        public void write(Message message)
        {
            lock(messages)
            {
                messages.Enqueue(message);
                Monitor.PulseAll(messages);
            }
        }

        protected override void doWork()
        {
            while (isAlive())
            {
                Message message = take();
                if (message != null)
                {
                    network.Send(message);
                }
            }
        }

        private Message take()
        {
            lock(messages)
            {
                while (isAlive() && messages.Count == 0)
                    Monitor.Wait(messages);

                if (!isAlive())
                    return null;

                return messages.Dequeue();
            }
        }
    }
}
