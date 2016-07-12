using System;
namespace soa
{
	public class AgentMessageHandler : IMessageHandler
	{
		private int agentID;
		private INetworkConnection connection;
		private ConnectMessageHandler connectHandler;
		private PostMessageHandler postHandler;
		private SyncMessageHandler syncHandler;

		public AgentMessageHandler(int agentID, BeliefRepository repo, INetworkConnection connection, Serializer serializer)
		{
			this.agentID = agentID;
            this.connection = connection;
			this.connectHandler = new ConnectMessageHandler(repo, this);
			this.postHandler = new PostMessageHandler(repo, this, serializer);
			this.syncHandler = new SyncMessageHandler(repo, this);
		}

		public int getAgentID()
		{
			return agentID;
		}

		public INetworkConnection getConnection()
		{
			return connection;
		}

		public void handleMessage(BSPMessage message)
		{
			switch (message.getType())
			{
				case BSPMessageType.CONNECT: connectHandler.handleMessage(message);
					break;
				case BSPMessageType.POST: postHandler.handleMessage(message);
					break;
				case BSPMessageType.SYNC: syncHandler.handleMessage(message);
					break;
			}
		}

		public void post(CachedBelief belief)
		{
			postHandler.post(belief);
		}

		public void synchronizeAllBeliefs()
		{
			syncHandler.synchronizeAllBeliefs();
		}

		public void synchronizeBelief(Belief belief)
		{
			syncHandler.synchronizeBelief(belief);
		}
	}
}

