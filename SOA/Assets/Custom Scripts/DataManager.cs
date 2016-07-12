// Additinonal using statements are needed if we are running in Unity
#if(UNITY_STANDALONE)
using UnityEngine;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class DataManager : IDataManager
    {
        //public float updateRateMs;
        //List of all actors in this scenario
        public List<SoaActor> actors = new List<SoaActor>();
        public List<Belief> initializationBeliefs = new List<Belief>();
        public SortedDictionary<int, SoaActor> soaActorDictionary = new SortedDictionary<int,SoaActor>();

        //Dictionary of belief data
        protected SortedDictionary<Belief.Key, SortedDictionary<int, Belief> > beliefDictionary;
        public System.Object dataManagerLock = new System.Object();
		private LocalCommManager cm;

        private string room;
        public readonly IPhysicalLayer<int> physicalNetworkLayer;

        // Constructor
        public DataManager(string roomName, int port=5055)
        {
            room = roomName;
            
            //Initialze belief manager with all environment data, the comms manager will broadcast this
            //data on cm.start()
            
            Serializer ps = new ProtobufSerializer();

            cm = new LocalCommManager(this, ps, new UdpNetwork(port));
            //cm = new PhotonCloudCommManager(this, ps, "app-us.exitgamescloud.com:5055", roomName, 0, 0);
            //cm = new PhotonCloudCommManager(dm, ps, "10.101.5.25:5055", "soa");

            beliefDictionary = new SortedDictionary<Belief.Key, SortedDictionary<int, Belief>>();
            physicalNetworkLayer = new IdealizedNetworkLayer(new DataManagerWorld(this));

            // Note: Comms manager must be started manually after all initial belief processing is done
        }

        public void startComms()
        {
            if (cm != null)
            {
                cm.start();
            }
        }

		public void synchronizeRepository(int sourceID)
		{
			cm.synchronizeBeliefsFor(sourceID);
		}

		public void synchronizeBelief(Belief b, int sourceID)
		{
			cm.synchronizeBelief(b, sourceID);
		}

        public string getConnectionInfoForAgent(int agentID)
        {
            return cm.getConnectionForAgent(agentID);
        }

        /*public void addAndBroadcastBelief(Belief b, int sourceId, int[] recipients)
        {
            if (cm != null)
            {
                cm.addOutgoing(b, sourceId, recipients);
                addBelief(b, sourceId);
            }
            
        }*/

        //Add incoming belief to correct agent
        public void addExternalBeliefToActor(Belief b, int sourceId)
        {
            SoaActor a;
            soaActorDictionary.TryGetValue(sourceId, out a);
            if (a != null)
            {
				a.addExternalBelief(b);
            }
        }

        public void addBeliefToAllActors(Belief b, int sourceId)
        {
            foreach (KeyValuePair<int, SoaActor> entry in soaActorDictionary)
            {
				entry.Value.addExternalBelief(b);
            }
        }


        // Check if belief is newer than current belief of matching type and id, if so,
        // replace old belief with b.
        public void addBeliefToDataManager(Belief b, int sourceId)
        {
            lock (dataManagerLock)
            {
                
                SortedDictionary<int, Belief> tempTypeDict = getBeliefsFor(b.getTypeKey());
                if (tempTypeDict != null)
                {
                    Belief oldBelief;
                    if (!getBeliefsFor(b.getTypeKey()).TryGetValue(b.getId(), out oldBelief) || oldBelief.getBeliefTime() < b.getBeliefTime())
                    {
                        getBeliefsFor(b.getTypeKey())[b.getId()] = b;
                    }
                }
                else
                {
                    getBeliefsFor(b.getTypeKey())[b.getId()] = b;
                }

                /*
                 * Do not update actors in this function
                SortedDictionary<int, bool> sourceDistanceDictionary;
                if (actorDistanceDictionary.TryGetValue(sourceId, out sourceDistanceDictionary))
                {
                    foreach (KeyValuePair<int, bool> entry in sourceDistanceDictionary)
                    {
                        SoaActor destActor = soaActorDictionary[entry.Key];
                        if (entry.Value)
                        {
                            destActor.addBelief(b, sourceId);
                        }
                    }
                }*/
            }
        }

        /*
         * Call this function once every update of the simulation time step to refresh the true position data for all the actors
         * A pair of actors actorDistanceDictionary[destId][sourceId] = true if the source comms range is large enough to reach the destination.
         */
        public void calculateDistances()
        {
            physicalNetworkLayer.Update();
        }

        /*
         * Add every new actor to the data manager list of actors
         */
        public void addNewActor(SoaActor actor)
        {
            if (!actors.Contains(actor))
            {
                actors.Add(actor);
                //Debug.Log("Adding actor " + actor.unique_id + " to actor dictionary");
                soaActorDictionary[actor.unique_id] = actor;
                cm.addNewActor(actor.unique_id, actor.getRepository());
                addBeliefToDataManager(
                    new Belief_Actor(actor.unique_id, (int)actor.affiliation, actor.type, actor.isAlive, 
                        actor.numStorageSlots, actor.numCasualtiesStored,
                        actor.numSuppliesStored, actor.numCiviliansStored,
                        actor.isWeaponized, actor.hasJammer, actor.fuelRemaining_s,
                        actor.transform.position.x / SimControl.KmToUnity,
                        actor.simAltitude_km,
                        actor.transform.position.z / SimControl.KmToUnity), 
                        actor.unique_id);
            }
            else
            {
                Debug.LogError("TRIED TO ADD ACTOR TO DATA MANAGER THAT ALREADY EXISTS: " + actor.unique_id);
            }
        }

        /*
         * Remove an existing actor from data manager list of actors
         */
        public void removeActor(SoaActor actor)
        {
            if (actors.Contains(actor))
            {
                actors.Remove(actor);
                Debug.Log("Removing actor " + actor.unique_id + " from actor dictionary");
                soaActorDictionary.Remove(actor.unique_id);
            }
            else
            {
                Debug.LogError("TRIED TO REMOVE ACTOR FROM DATA MANAGER THAT DOESN'T EXIST: " + actor.unique_id);
            }
        }

        public SortedDictionary<Belief.Key, SortedDictionary<int, Belief>> getGodsEyeView()
        {
            return beliefDictionary;
        }

        public void stopPhoton()
        {
            if (cm != null)
                cm.terminate();
        }

        // Initialization beliefs are kept separately from actual belief manager beliefs
        // in case a player joins mid game
        public void addInitializationBelief(Belief b)
        {
            initializationBeliefs.Add(b);
        }

        // This method returns all Beliefs that a new player needs to initialize itself
        // such as map/terrain description, etc.  These beliefs do not change over time.
        public List<Belief> getInitializationBeliefs()
        {
            return initializationBeliefs;
        }

        private SortedDictionary<int, Belief> getBeliefsFor(Belief.BeliefType type)
        {
            return getBeliefsFor(Belief.keyOf(type));
        }

        private SortedDictionary<int, Belief> getBeliefsFor(Belief.Key key)
        {
            SortedDictionary<int, Belief> beliefs;
            if (!beliefDictionary.TryGetValue(key, out beliefs))
            {
                beliefs = new SortedDictionary<int, Belief>();
                beliefDictionary[key] = beliefs;
            }
            return beliefs;
        }

        private class DataManagerWorld : IWorld
        {
            private readonly DataManager manager;

            public DataManagerWorld(DataManager manager)
            {
                this.manager = manager;
            }

            public IEnumerable<ISoaActor> getActors()
            {
                //.Net 2.0 is terrible at generics
                return manager.actors.Cast<ISoaActor>();
            }

            public IEnumerable<ISoaJammer> getJammers()
            {
                return SimControl.jammers.Cast<ISoaJammer>();
            }
        }
    }
}