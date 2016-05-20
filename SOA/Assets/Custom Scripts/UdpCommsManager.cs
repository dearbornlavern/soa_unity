using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace soa
{

    public class UdpCommsManager
    {

        public const int UDP_MAX_PACKET_SIZE = 65500;

        // Reference to data manager
        private DataManager dataManager;

        // Reference to serializer
        private Serializer serializer;

        // Connection information
        private string multicastAddress;
        private int multicastPort;

        // Sleep time between Photon server updates
        private int updateSleepTime_ms;

        // Default source ID to send along with Belief to data manager
        private int defaultSourceID;

        // The actor number we want to use when connecting to room
        private int actorNumber;

        // Photon
        private Thread receiveThread;
        private Thread sendThread;

        // Outgoing messages
        private Queue<Byte[]> outgoingQueue;
        private Queue<int[]> outgoingTargetQueue;
        private Queue<Byte[]> localQueue;
        private Queue<int[]> localTargetQueue;

        // Photon
        private Thread updateThread;


        // Settings for managing outgoing queue
        private int maxQueueSize;
        private bool overwriteWhenQueueFull;

        private bool terminateRequested;

        private Socket sockUdpListener;
        private UdpClient sockSender;


        private IPEndPoint localEP;
        private IPEndPoint remoteEP;
        private EndPoint local;

        byte[] dataIn = new byte[UDP_MAX_PACKET_SIZE];


        public UdpCommsManager(DataManager dataManager, Serializer serializer,
        string multicastAddress, int multicastPort, int defaultSourceID, int actorNumber = 0, // actorNumber = 0 for randomly assigned
        int updateSleepTime_ms = 100, int maxQueueSize = 1000000, bool overwriteWhenQueueFull = true)
        {
            // Copy variables
            this.dataManager = dataManager;
            this.serializer = serializer;
            this.multicastAddress = multicastAddress;
            this.multicastPort = multicastPort;
            this.defaultSourceID = defaultSourceID;
            this.actorNumber = actorNumber;
            this.updateSleepTime_ms = updateSleepTime_ms;
            this.maxQueueSize = maxQueueSize;
            this.overwriteWhenQueueFull = overwriteWhenQueueFull;

            // Initialize queue
            outgoingQueue = new Queue<Byte[]>();
            outgoingTargetQueue = new Queue<int[]>();
            localQueue = new Queue<Byte[]>();
            localTargetQueue = new Queue<int[]>();
        }


        // Use this for initialization
        void Start()
        {
            Debug.Log("Starting Udp Multicast Socket");
            startUdpMulticastServer();
        }


        public void startUdpMulticastServer()
        {

            /*
             * EnableTimers(false);
             */

            sockUdpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockUdpListener.Blocking = true;

            //local port is multicast port if using multicast
            localEP = new IPEndPoint(IPAddress.Any, multicastPort);

            //remote address and port are multicast address and port if multicast is enabled
            remoteEP = new IPEndPoint(IPAddress.Parse(multicastAddress), multicastPort);

            sockUdpListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            sockUdpListener.ReceiveBufferSize = UDP_MAX_PACKET_SIZE;


            IPAddress multicastGroup = IPAddress.Parse(multicastAddress);
            sockUdpListener.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastGroup, IPAddress.Any));

            sockSender = new UdpClient();
            sockSender.EnableBroadcast = true;
            sockSender.JoinMulticastGroup(IPAddress.Parse(multicastAddress));

            receiveUpdateThread();
            sendUpdateThread();

            local = (EndPoint)localEP;
        }

        private void sendUpdateThread()
        {
            // When starting send thread, add initializatoin beliefs to outgoing
            addOutgoing(dataManager.getInitializationBeliefs(), -1, null);

            // Keep looping until conditions are met to terminate and exit fsm
            while (!terminateRequested)
            {

                // Normal operation, take appropriate action based on current connectivity state
                sendOutgoing();

                // Wait a while before talking with server again
                System.Threading.Thread.Sleep(updateSleepTime_ms);
            }
        }

        private void udpSendPacket(byte[] data, int size)
        {
            if (sockSender != null)
            {
                sockSender.Send(data, size, remoteEP);

            }

        }

        private void receiveUpdateThread()
        {

            if (sockUdpListener != null && !terminateRequested)
            {
                try
                {
                    {
                        Debug.Log("Udp Socket begin recieve from");

                        sockUdpListener.BeginReceiveFrom(dataIn, 0, dataIn.Length, SocketFlags.None, ref local, new AsyncCallback(ReceiveData), sockUdpListener);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    Debug.Log(e.Message);
                }
            }

        }


        void ReceiveData(IAsyncResult iar)
        {
            Socket sockRemote = (Socket)iar.AsyncState;
            int recv = sockRemote.EndReceiveFrom(iar, ref local);
            byte[] data = new byte[recv];
            Array.Copy(dataIn, data, recv);

            sockUdpListener.BeginReceiveFrom(dataIn, 0, dataIn.Length, SocketFlags.None, ref local, new AsyncCallback(ReceiveData), sockUdpListener);

            processReceivePacket(data);


        }



        /// <summary>
        /// Adds multiple beliefs from data manager to outgoing queue using default source ID
        /// directed for specified actor
        /// Use null for targetActorIDs if broadcast
        /// </summary>
        public void addOutgoing(List<Belief> l, int[] targetActorIDs)
        {
            addOutgoing(l, defaultSourceID, targetActorIDs);
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

            // Serialize the belief
            if (b.getBeliefType().Equals(Belief.BeliefType.CUSTOM))
            {
                //Debug.LogWarning("PUSING CUSTOM BELIEF to " + sourceID);
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
#if(UNITY_STANDALONE)
                Debug.Log("PhotonCloudCommManager: Belief serialization failed");
#else
                Console.Error.WriteLine("PhotonCloudCommManager: Belief serialization failed");
#endif
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
                //Dictionary<byte, object> opParams = new Dictionary<byte, object>();
                //Hashtable evData = new Hashtable();
                //evData[(byte)0] = packet;


                // Everything is on broadcast now
                udpSendPacket(packet, packetLength);

            }
        }

        /// <summary>
        /// Callback to handle messages received over Photon game network
        /// Processed within PhotonPeer.DispatchIncomingCommands()!
        /// </summary>
        public void processReceivePacket(Byte[] packet)
        {
            //receive packet of new player joining, send out initialization beliefs
            if (packet[0] == 0xFF)
            {
                // Someone else just joined the room, print status message
                addOutgoing(dataManager.getInitializationBeliefs(), -1, null);

            }
            else
            {
                // Received a Protobuff message
                // Message = 1 byte for length of rest of message, 4 bytes for source ID, rest is serialized belief
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
#if(UNITY_STANDALONE)
                    Debug.Log("PhotonCloudCommManager: Belief deserialization failed");
#else
                            Console.Error.WriteLine("PhotonCloudCommManager: Belief deserialization failed");
#endif
                }



            }

        }


        /// <summary>
        /// Sends signal to shut down the communication manager 
        /// </summary>
        public void terminate()
        {
            terminateRequested = true;
            // Wait for termination
            Thread.Sleep(1);
            sockUdpListener.Disconnect(true);
            sendThread.Join();
            receiveThread.Join();
        }
    }
}
