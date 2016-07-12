using System;
using System.Net;

namespace soa
{
	public enum BSPMessageType
	{
		CONNECT	= 0,
        POST	= 1,
		SYNC	= 2,
        UNKNOWN	=-1
	}

	public class BSPMessage
	{
		private IPEndPoint address;
        private BSPMessageType type;
        private int sourceID;
        private NetworkBuffer messageData;

        public BSPMessage(IPEndPoint address, BSPMessageType type, int sourceID, NetworkBuffer messageData)
        {
			this.address = address;
			this.type = type;
			this.sourceID = sourceID;
			this.messageData = messageData;
        }

		public IPEndPoint getAddress()
		{
			return address;
		}

		public BSPMessageType getType()
		{
			return type;
		}

		public int getSourceID()
		{
			return sourceID;
		}

		public NetworkBuffer getData()
		{
			return messageData;
		}
	}
}

