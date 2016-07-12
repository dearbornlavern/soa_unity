using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace soa
{
    public class Message
    {
        public readonly IPEndPoint address;
        public readonly byte[] data;
        
        public Message(IPEndPoint address, byte[] data)
        {
            this.address = address;
            this.data = data;
        }
    }

    public interface INetwork
    {
        void Start();

        void Stop();

        void Send(Message message);
        
        Message Receive();
    }
}
