using System.Collections.Generic;

namespace soa
{
    class ConnectivityGraph
    {
		private const int MAX_HOPS = int.MaxValue;
		private readonly IWorld world;
        private Dictionary<int, ActorNode> nodes;

		public ConnectivityGraph(IWorld world)
        {
			this.world = world;
            nodes = new Dictionary<int, ActorNode>();
        }

        public void UpdateConnectivity()
        {
            lock(nodes)
            {
				//Step 1: Rebuild nodes for all actors
				InitializeNodes();
				//Step 2: Compute who can talk based on SNR
				ComputeNeighbors();
				//Step 3: Cache the cliques for each node
				BuildCliques();
            }
        }

        public ICollection<ISoaActor> GetActorsInCliqueOf(int actorID)
        {
			lock (nodes)
			{
				ActorNode node = null;
				if (nodes.TryGetValue(actorID, out node))
				{
					return node.clique;
				}
				return new List<ISoaActor>(0);
			}
        }

		private void BuildCliques()
		{
			foreach (ActorNode node in nodes.Values)
			{
				HashSet<ISoaActor> clique = new HashSet<ISoaActor>();
				clique.Add(node.actor);
				BuildClique(clique, node, 0);
				clique.Remove(node.actor);//Don't talk to yourself
				node.clique = new List<ISoaActor>(clique);
			}	
		}

        private void BuildClique(HashSet<ISoaActor> clique, ActorNode node, int depth)
        {
            if (depth > MAX_HOPS)
            {
                return;
            }

            foreach (ActorNode neighbor in node.neighbors)
            {
                if (clique.Add(neighbor.actor))
                {
                    BuildClique(clique, neighbor, depth + 1);
                }
            }
        }

        private void InitializeNodes()
        {
            nodes.Clear();
			initializeNodes(world.getActors());
        }

		private void initializeNodes(IEnumerable<ISoaActor> actors)
		{
			foreach (ISoaActor actor in actors)
			{
				ActorNode node = new ActorNode(actor);
				computeJammerNoise(node);
				nodes.Add(actor.getID(), node);
			}
		}

        private void ComputeNeighbors()
        {
			computeNeighbors(world.getActors());
        }

		private void computeNeighbors(IEnumerable<ISoaActor> team)
		{
			foreach (ISoaActor soaActor in team)
			{
				ActorNode node = nodes[soaActor.getID()];

				foreach (ISoaActor neighborActor in team)
				{
					if (soaActor == neighborActor)
						continue;

					ActorNode neighborNode = nodes[neighborActor.getID()];

					if (canReceiveSignal(node, neighborNode))
					{
						neighborNode.canSendTo(node);
					}
				}
			}
		}

		private bool canReceiveSignal(ActorNode actor, ActorNode neighbor)
        {
            if (hasWiredConnection(actor, neighbor))
            {
                return true;
            }

			float rx_tx_range_km = actor.actorPos_km.distance(neighbor.actorPos_km);
            float rangeSquared_km2 = rx_tx_range_km * rx_tx_range_km;
			float commsRange_km = actor.actor.getCommsRangeKM();
            float snr = (commsRange_km * commsRange_km) / (rangeSquared_km2 * (1 + actor.totalNoise));
            return snr >= 1;
        }

		private void computeJammerNoise(ActorNode actor)
		{
			actor.totalNoise = 0;
			foreach (ISoaJammer jammerActor in world.getJammers())
			{
				PositionKM jammerPos_km = jammerActor.getActor().getPositionInKilometers();
				float jammerToActorDist_km = actor.actorPos_km.distance(jammerPos_km);
				float effRangeSq = jammerActor.getEffectiveRangeKm() * jammerActor.getEffectiveRangeKm();
				float distSq = jammerToActorDist_km * jammerToActorDist_km;
				actor.totalNoise += effRangeSq / distSq;
			}
		}

        private bool hasWiredConnection(ActorNode actor, ActorNode neighbor)
        {
            return (actor.isBalloon() && neighbor.isBaseStation())
                    || (actor.isBaseStation() && neighbor.isBalloon());
        }

        private class ActorNode
        {
            public readonly ISoaActor actor;
            public readonly List<ActorNode> neighbors;//list because iterating is more important than insertion
			public readonly PositionKM actorPos_km;
            public float totalNoise;
			public List<ISoaActor> clique;

			public ActorNode(ISoaActor actor)
            {
                this.actor = actor;
                neighbors = new List<ActorNode>();
				actorPos_km = actor.getPositionInKilometers();
				totalNoise = float.MaxValue;
				clique = new List<ISoaActor>(0);
            }

            public void canSendTo(ActorNode node)
            {
                if (!neighbors.Contains(node))
                {
                    neighbors.Add(node);
                }
            }

            public bool isBalloon()
            {
				return actor.isBalloon();
            }

            public bool isBaseStation()
            {
				return actor.isBaseStation();
            }
        }
    }
}
