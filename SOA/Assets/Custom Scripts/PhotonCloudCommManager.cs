// Additinonal using statements are needed if we are running in Unity
#if(UNITY_STANDALONE)
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;

namespace soa
{
    /// <summary>
    /// Facilitates communication of Belief objects over Photon Cloud network.
    /// </summary>
    public class PhotonCloudCommManager : LoadBalancingClient, ICommManager
    {
        // Reference to data manager
        private DataManager dataManager;

        // Reference to serializer
        private Serializer serializer;

        // Connection information
        private string ipAddress;
        private string roomName;

        // Sleep time between Photon server updates
        private int updateSleepTime_ms;

        // Default source ID to send along with Belief to data manager
        private int defaultSourceID;

        // The actor number we want to use when connecting to room
        private int actorNumber;

        // Outgoing messages
        private Queue<Byte[]> outgoingQueue;
        private Queue<int[]> outgoingTargetQueue;
        private Queue<Byte[]> localQueue;
        private Queue<int[]> localTargetQueue;

        // Photon
        private Thread photonUpdateThread;

        // Keep track of internal states
        private ClientState prevState;

        // Settings for managing outgoing queue
        private int maxQueueSize;
        private bool overwriteWhenQueueFull;

        // Internal State
        private bool terminateRequested, startEligible;
        private System.Object startEligibleMutex;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotonCloudCommManager"/> class. 
        /// </summary>
        public PhotonCloudCommManager(DataManager dataManager, Serializer serializer,
            string ipAddress, string roomName, int defaultSourceID, int actorNumber = 0, // actorNumber = 0 for randomly assigned
            int updateSleepTime_ms = 100, int maxQueueSize = 1000000, bool overwriteWhenQueueFull = true)
            : base()
        {
            // Copy variables
            this.dataManager = dataManager;
            this.serializer = serializer;
            this.ipAddress = ipAddress;
            this.roomName = roomName;
            this.defaultSourceID = defaultSourceID;
            this.actorNumber = actorNumber;
            this.updateSleepTime_ms = updateSleepTime_ms;
            this.maxQueueSize = maxQueueSize;
            this.overwriteWhenQueueFull = overwriteWhenQueueFull;

            // Initialize mutex
            startEligibleMutex = new System.Object();

            // Initialize queue
            outgoingQueue = new Queue<Byte[]>();
            outgoingTargetQueue = new Queue<int[]>();
            localQueue = new Queue<Byte[]>();
            localTargetQueue = new Queue<int[]>();

            // Initialize prev states
            prevState = State;

            // Used to keep track of whether a new thread can start
            startEligible = true;

            // Used to keep track of whether termination request has been sent
            terminateRequested = false;

            // Initialize thread references
            photonUpdateThread = null;

            // Insert your game's AppID (replace <InsertYourAppIdHere>). 
            // Hosting yourself: use any name. 
            // Using Photon Cloud: use your cloud subscription's appID
            this.AppId = "36a65149-f55c-4395-b2db-33bff4de9282";
            this.AppVersion = "1.0";
            this.MasterServerAddress = ipAddress;
        }

        public void addNewActor(int actorID, BeliefRepository repo)
        {

        }
        public string getConnectionForAgent(int agentID)
        {
            return "";
        }

        /// <summary>
        /// Starts the communication manager 
        /// </summary>
        public void start()
        {
            // Acquire lock
            lock (startEligibleMutex)
            {
                // Start photon update thread if not already running
                if (startEligible)
                {
                    // Cannot start another one
                    startEligible = false;

                    // Do not terminate the fsm
                    terminateRequested = false;

                    // Create and start new thread
                    photonUpdateThread = new Thread(new ThreadStart(photonUpdate));
                    photonUpdateThread.Start();

                    // Wait for it to start up
                    while (!photonUpdateThread.IsAlive) ;
                    Log.debug("PhotonCloudCommManager: Update thread started");
                }
                else
                {
                    // Photon update thread already running, no need to do anything else
                    Log.debug("PhotonCloudCommManager: start() has no effect, update thread already running");
                }
            } // Unlock
        }

        /// <summary>
        /// Sends signal to shut down the communication manager 
        /// </summary>
        public void terminate()
        {
            // Acquire lock
            lock (startEligibleMutex)
            {
                // Stop photon thread if currently running
                if (!startEligible)
                {
                    // Request to disconnect has been sent
                    Log.debug("PhotonCloudCommManager: Termination request sent");
                    terminateRequested = true;

                    // Wait for termination
                    photonUpdateThread.Join();

                    // Since thread has terminated, can now start another
                    startEligible = true;

                    // Notify that comm manager has been terminated
                    Log.debug("PhotonCloudCommManager: Termination successful");
                }
                else
                {
                    // Photon update thread already inactive, no need to do anything else
                    Log.debug("PhotonCloudCommManager: terminate() has no effect, update thread already inactive");
                }
            } // Unlock
        }

        /// <summary>
        /// Thread method that implements a finite InternalState machine to connect to photon server and send outgoing messages periodically
        /// </summary>
        private void photonUpdate()
        {
            // Keep looping until conditions are met to terminate and exit fsm
            while (!(terminateRequested && (State == ClientState.Uninitialized || State == ClientState.Disconnected)))
            {
                // Print internal state
                if (prevState != State)
                {
                    Log.debug("PhotonCloudCommManager: " + prevState + " -> " + State);
                    prevState = State;
                }

                if (terminateRequested)
                {
                    // Just a single call to disconnect
                    Disconnect();
                }
                else
                {
                    // Normal operation, take appropriate action based on current connectivity state
                    switch (State)
                    {
                        case ClientState.Disconnected:
                        case ClientState.Uninitialized:
                            // Connect to a Photon server
                            Connect();
                            break;
                        case ClientState.JoinedLobby:
                            // Create or join our room
                            OpJoinOrCreateRoom(roomName, actorNumber, new RoomOptions());
                            ChangeLocalID(1);
                            break;
                        case ClientState.Joined:
                            // Within room, send any queued outgoing messages
                            if (this.LocalPlayer.ID != 1) Debug.LogError("ERROR: ID IS NOT 1");
                            sendOutgoing();
                            break;
                    }
                }

                // Communicate with Photon server
                Service();

                // Wait a while before talking with server again
                System.Threading.Thread.Sleep(updateSleepTime_ms);
            }
        }
        
        /// <summary>
        /// Adds multiple beliefs from data manager to outgoing queue using specified source ID
        /// directed for specified actor
        /// Use null for targetActorIDs if broadcast
        /// </summary>
        public void addOutgoing(List<Belief> l, int sourceID, int[] targetActorIDs)
        {
            foreach (Belief b in l)
            {
                addOutgoing(b, sourceID, targetActorIDs);
            }
        }

        public void addOutgoing(CachedBelief b, int sourceID, int[] targetActorIDs)
        {
            addOutgoing(b.GetBelief(), sourceID, targetActorIDs);
        }

        /// <summary>
        /// Adds information from data manager to outgoing queue using specified source ID
        /// Use null for targetActorIDs if broadcast
        /// </summary>
        public void addOutgoing(Belief b, int sourceID, int[] targetActorIDs)
        {
            // Serialize the 4-byte source ID, network byte order (Big Endian)
            Byte[] sourceIDBytes = BitConverter.GetBytes(sourceID);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sourceIDBytes);
            }

            Byte[] beliefBytes = serializer.serializeBelief(b);

            // Enqueue the serialized message if serialization was
            // successful (if serial is nonempty)
            if (beliefBytes.Length > 0)
            {
                // Combine the serialized source ID and belief into one message
                Byte[] message = new Byte[4 + beliefBytes.Length];
                System.Buffer.BlockCopy(sourceIDBytes, 0, message, 0, 4);
                System.Buffer.BlockCopy(beliefBytes, 0, message, 4, beliefBytes.Length);
                
                lock (outgoingQueue)
                {
                    if (outgoingQueue.Count < maxQueueSize)
                    {
                        // There is still space in queue, go ahead and enqueue
                        outgoingQueue.Enqueue(message);
                        outgoingTargetQueue.Enqueue(targetActorIDs);
                    }
                    else if (outgoingQueue.Count == maxQueueSize && overwriteWhenQueueFull)
                    {
                        // No more space left and our policy is to overwrite old data
                        // Dequeue the oldest entry before enqueuing the new data
                        outgoingQueue.Dequeue();
                        outgoingTargetQueue.Dequeue();
                        outgoingQueue.Enqueue(message);
                        outgoingTargetQueue.Enqueue(targetActorIDs);
                    }
                    else
                    {
                        // No space left, policy says to do nothing and drop newest entry
                    }
                } // Unlock
            }
            else
            {
                // Something went wrong with serialization
                Log.debug("PhotonCloudCommManager: Belief serialization failed");
            }
        }

        /// <summary>
        /// Sends any outgoing messages that are on the queue
        /// </summary>
        private void sendOutgoing()
        {
            // Target player
            int[] targetPlayerIDs;

            // Buffers
            Byte[] message;
            Byte[] packet;

            // Acquire lock
            lock (outgoingQueue)
            {
                // Take everything from outgoing queue and put into local queue
                // to minimize time spent with lock (in case something causes
                // sending over Photon to hang
                while (outgoingQueue.Count > 0)
                {
                    // Pop out first element in queue
                    message = outgoingQueue.Dequeue();
                    targetPlayerIDs = outgoingTargetQueue.Dequeue();

                    // Push into local queue
                    localQueue.Enqueue(message);
                    localTargetQueue.Enqueue(targetPlayerIDs);
                }
            } // Unlock

            // Now send out everything that is in the local queue
            while (localQueue.Count > 0)
            {
                // Pop out first element in queue
                message = localQueue.Dequeue();
                targetPlayerIDs = localTargetQueue.Dequeue();

                // Header is serialized 4-byte message length, network byte order (Big Endian)
                Byte[] headerBytes = BitConverter.GetBytes(message.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(headerBytes);
                }

                // Create packet by appending header to message
                int packetLength = message.Length + 4;
                packet = new Byte[packetLength];
                System.Buffer.BlockCopy(headerBytes, 0, packet, 0, 4);
                System.Buffer.BlockCopy(message, 0, packet, 4, message.Length);

                // Create data to be sent
                Dictionary<byte, object> opParams = new Dictionary<byte, object>();
                Hashtable evData = new Hashtable();
                evData[(byte)0] = packet;

                // Either broadcast or send to select players based on targetPlayerIDs
                if (targetPlayerIDs == null || targetPlayerIDs.Length == 0)
                {
                    // No target player IDs specified, broadcast to all
                    OpRaiseEvent(0, evData, true, null);
                }
                else
                {
                    // Specific player IDs specified, only send to them
                    RaiseEventOptions ro = new RaiseEventOptions();
                    ro.TargetActors = targetPlayerIDs;
                    OpRaiseEvent(0, evData, true, ro);
                }                
            }
        }

        /// <summary>
        /// Callback to handle messages received over Photon game network
        /// Processed within PhotonPeer.DispatchIncomingCommands()!
        /// </summary>
        public override void OnEvent(EventData photonEvent)
        {
            // Call original OnEvent()
            base.OnEvent(photonEvent);

            // Handle our custom event
            switch (photonEvent.Code)
            {
                case 0: // Received a Protobuff message
                    {
                        // Get the byte[] message from protobuf
                        // Message = 1 byte for length of rest of message, 4 bytes for source ID, rest is serialized belief
                        Hashtable evData = (Hashtable)photonEvent.Parameters[ParameterCode.Data];
                        Byte[] packet = (Byte[])evData[(byte)0];

                        // Extract 4-byte message length (convert from network byte order (Big Endian) to native order if necessary)
                        Byte[] messageLengthBytes = new Byte[4];
                        System.Buffer.BlockCopy(packet, 0, messageLengthBytes, 0, 4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(messageLengthBytes);
                        }
                        int messageLength = BitConverter.ToInt32(messageLengthBytes, 0);

                        // Extract 4-byte source ID (convert from network byte order (Big Endian) to native order if necessary)
                        Byte[] sourceIDBytes = new Byte[4];
                        System.Buffer.BlockCopy(packet, 4, sourceIDBytes, 0, 4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(sourceIDBytes);
                        }
                        int sourceID = BitConverter.ToInt32(sourceIDBytes, 0);

                        // Get belief length
                        int serializedBeliefLength = messageLength - 4;

                        // Extract serialized belief
                        Byte[] serializedBelief = new Byte[serializedBeliefLength];
                        System.Buffer.BlockCopy(packet, 8, serializedBelief, 0, serializedBeliefLength);

                        // Extract the belief
                        Belief b = serializer.generateBelief(serializedBelief);

                        // If deserialization was successful
                        if (b != null)
                        {
                            // Add the belief to the data manager if it passed filter, no filter for Unity
                            dataManager.addExternalBeliefToActor(b, sourceID);
                        }
                        else
                        {
                            // Something went wrong with deserialization
                            Log.debug("PhotonCloudCommManager: Belief deserialization failed");
                        }
                        break;
                    }
                case EventCode.Join: // Someone joined the game
                    {
                        // Get ID of actor that joined
                        int actorNrJoined = (int)photonEvent.Parameters[ParameterCode.ActorNr];

                        // Determine if the player that joined is myself
                        bool isLocal = (this.LocalPlayer.ID == actorNrJoined);

                        // Get initialization beliefs
                        List<Belief> initializationBeliefs = dataManager.getInitializationBeliefs();

                        // Only send if not null or empty list
                        bool sendInitializationBeliefs = (initializationBeliefs != null ) && (initializationBeliefs.Count > 0);

                        if (isLocal)
                        {
                            // I just joined the room, broadcast initialization beliefs to everyone
                            // Use -1 source ID to override comms distances constraints
                            if (sendInitializationBeliefs)
                            {
                                addOutgoing(initializationBeliefs, -1, null);
                            }
                        }else{
                            // Someone else just joined the room, print status message
                            Log.debug("PhotonCloudCommManager: Player " + actorNrJoined + " just joined");

                            // Get initialization beliefs and only send to that player
                            // Use -1 source ID to override comms distances constraints
                            if(sendInitializationBeliefs){
                                addOutgoing(initializationBeliefs, -1, new int[] {actorNrJoined});
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Write to console for debugging output. 
        /// </summary>
        private void DebugReturn(string debugStr)
        {
        }
    }
}
