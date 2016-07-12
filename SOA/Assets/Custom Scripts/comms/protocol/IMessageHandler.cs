using System;
namespace soa
{
	public interface IMessageHandler
	{
		void handleMessage(BSPMessage message);
	}
}

