using System;
using System.Net;

namespace soa
{
	public interface INetworkConnection
	{

		void setRemoteAddress(IPEndPoint address);

		IPEndPoint getRemoteAddress();

		void send(BSPMessage message);

	}
}

