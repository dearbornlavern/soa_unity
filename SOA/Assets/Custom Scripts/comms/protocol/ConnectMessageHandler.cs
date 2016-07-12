using System;
namespace soa
{
	public class ConnectMessageHandler : IMessageHandler
	{
		private readonly BeliefRepository repo;
		private readonly AgentMessageHandler protocol;

		public ConnectMessageHandler(BeliefRepository repo, AgentMessageHandler protocol)
		{
			this.repo = repo;
			this.protocol = protocol;
		}

		public void handleMessage(BSPMessage message)
		{
			protocol.getConnection().setRemoteAddress(message.getAddress());
            
            foreach(CachedBelief belief in repo.GetAllCachedBeliefs())
            {
                protocol.post(belief);
            }
        }
	}
}

