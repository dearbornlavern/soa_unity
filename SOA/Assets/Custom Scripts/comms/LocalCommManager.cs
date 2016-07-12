using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace soa
{
    public class LocalCommManager : ICommManager
    {
        private IDataManager dataManager;
        private Dictionary<int, IPEndPoint> agentIPs;
        private INetwork network;
        private readonly MessageReader reader;
        private readonly MessageWriter writer;
        private readonly Serializer serializer;
		private BeliefSyncProtocol protocol;

        public LocalCommManager(IDataManager dataManager, Serializer serializer, INetwork network)
        {
            this.dataManager = dataManager;
            this.network = network;
            this.serializer = serializer;
            this.agentIPs = new Dictionary<int, IPEndPoint>();
            this.writer = new MessageWriter(network);
            this.reader = new MessageReader(handleRequest, network);
			this.protocol = new BeliefSyncProtocol(writer, serializer);
        }

        public void start()
        {
            network.Start();
            reader.Start();
            writer.Start();
        }

        public void terminate()
        {
            network.Stop();
            reader.Stop();
            writer.Stop();
        }

		public void addNewActor(int actorID, BeliefRepository repo)
        {
			protocol.addActor(actorID, repo);
        }

		public void synchronizeBeliefsFor(int agentID)
		{
			protocol.synchronizeAllBeliefsFor(agentID);
		}

		public void synchronizeBelief(Belief b, int sourceID)
		{
			protocol.synchronizeBelief(b, sourceID);
		}

        public string getConnectionForAgent(int agentID)
        {
			return protocol.getConnectionString(agentID);
        }

		private void handleRequest(Message message)
        {
			protocol.handleMessage(message);   
        }

        private IPEndPoint getActorIPAddres(int agentID)
        {
            lock(agentIPs)
            {
                IPEndPoint address = null;
                agentIPs.TryGetValue(agentID, out address);
                return address;
            }
        }
    }
}

