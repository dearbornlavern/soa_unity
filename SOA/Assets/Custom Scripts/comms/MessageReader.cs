using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace soa
{
    public class MessageReader : ThreadWorker
    {
		public delegate void Callback(Message message);

        private readonly Callback callback;
        private readonly INetwork network;

        public MessageReader(Callback callback, INetwork network)
        {
            this.callback = callback;
            this.network = network;
        }

        protected override void doWork()
        {
            while (isAlive())
            {
                try
                {
                    readNextMessage();
                }
                catch (Exception e)
                {
                    Log.error(e.ToString());
                }
            }
        }

        private void readNextMessage()
        {
            Message message = network.Receive();
            if (message != null)
            {
				callback(message);
            }
        }
    }
}
