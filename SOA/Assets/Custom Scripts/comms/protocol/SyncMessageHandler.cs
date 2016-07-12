using System;
using System.Collections.Generic;

namespace soa
{
	public class SyncMessageHandler : IMessageHandler
	{
		private readonly BeliefRepository repo;
		private readonly AgentMessageHandler protocol;
		private RepositoryState remoteState;

		public SyncMessageHandler(BeliefRepository repo, AgentMessageHandler protocol)
		{
			this.repo = repo;
			this.protocol = protocol;
            remoteState = new RepositoryState(-1);
		}

		public void handleMessage(BSPMessage message)
		{
            RepositoryStateSerializer serializer = new RepositoryStateSerializer();
			RepositoryState incomingState = serializer.deserialize(message.getData());
			if (incomingState.RevisionNumber() > remoteState.RevisionNumber())
			{
                this.remoteState = incomingState;
				IEnumerable<CachedBelief> changedBeliefs = repo.Diff(remoteState);
				foreach (CachedBelief belief in changedBeliefs)
				{
					protocol.post(belief);
				}
			}
		}

		public void synchronizeAllBeliefs()
		{
            if (protocol.getConnection().getRemoteAddress() == null)
            {
                return;
            }
            
 			RepositoryStateSerializer serializer = new RepositoryStateSerializer();
			NetworkBuffer buffer = serializer.serialize(repo.CurrentState());
			BSPMessage message = new BSPMessage(protocol.getConnection().getRemoteAddress(),
			                                    BSPMessageType.SYNC,
			                                    protocol.getAgentID(),
			                                    buffer);
			protocol.getConnection().send(message);
		}

		public void synchronizeBelief(Belief belief)
		{
			IEnumerable<CachedBelief> changedBeliefs = repo.Diff(remoteState);
			foreach (CachedBelief cached in changedBeliefs)
			{
				if (cached.GetBelief().getTypeKey() == belief.getTypeKey()
				    && cached.GetBelief().getId() == belief.getId())
				{
					protocol.post(cached);
					break;
				}
			}
		}
	}
}

