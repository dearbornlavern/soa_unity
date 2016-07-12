using System;
using System.Collections.Generic;
using System.Net;

namespace soa
{
	public class BeliefSyncProtocol
	{
        private const int HEADER_LENGTH = 8;
        private const int HEADER_TYPE_OFFSET = 0;
        private const int HEADER_SOURCE_OFFSET = 4;

        private Dictionary<int, AgentMessageHandler> handlers;
		private IMessageWriter messageWriter;
		private readonly Serializer serializer;

		public BeliefSyncProtocol(IMessageWriter messageWriter, Serializer serializer)
		{
			this.handlers = new Dictionary<int, AgentMessageHandler>();
			this.messageWriter = messageWriter;
			this.serializer = serializer;
		}

		public void synchronizeAllBeliefsFor(int actorID)
		{
			AgentMessageHandler handler = findHandler(actorID);
			if (handler != null)
			{
				handler.synchronizeAllBeliefs();
			}
		}

		public void synchronizeBelief(Belief belief, int sourceID)
		{
			AgentMessageHandler handler = findHandler(sourceID);
			if (handler != null)
			{
				handler.synchronizeBelief(belief);
			}
		}

		public string getConnectionString(int actorID)
		{
			AgentMessageHandler handler = findHandler(actorID);
			if (handler != null)
			{
				IPEndPoint address = handler.getConnection().getRemoteAddress();
				if (address != null)
				{
					return address.ToString();
				}
			}
			return "Not Connected";
		}

		public void addActor(int actorID, BeliefRepository repo)
		{
			INetworkConnection connection = new AgentConnection(this, actorID);
			handlers[actorID] = new AgentMessageHandler(actorID, repo, connection, serializer);
		}

		public void handleMessage(Message message)
		{
			BSPMessage bspMessage = parse(message);
            AgentMessageHandler handler = null;
			if (handlers.TryGetValue(bspMessage.getSourceID(), out handler))
			{
				handler.handleMessage(bspMessage);
			}
		}

		private AgentMessageHandler findHandler(int actorID)
		{
			AgentMessageHandler handler = null;
			handlers.TryGetValue(actorID, out handler);
			return handler;
		}

		private Message formatMessage (BSPMessage data)
		{
			if (data == null) {
				Console.Error.WriteLine ("Could not format null data");
				return null;
			}

			int messageLength = data.getData().size();
			NetworkBuffer messageData = new NetworkBuffer(HEADER_LENGTH + messageLength);
			messageData.writeInt32 (HEADER_TYPE_OFFSET, (int)data.getType());
			messageData.writeInt32 (HEADER_SOURCE_OFFSET, data.getSourceID());

			if (data.getData().size() > 0) {
				messageData.writeBytes(data.getData().getBuffer(), 0, HEADER_LENGTH, data.getData().size());
			}
		
            return new Message(data.getAddress(), messageData.getBuffer());
        }

        private BSPMessage parse(Message message)
        {
			if (message == null) {
				Console.Error.WriteLine ("Could not parse null message");
				return null;
			}

            NetworkBuffer buffer = new NetworkBuffer(message.data);

            if (buffer.size() < HEADER_LENGTH) {
                throw new Exception("Invalid message: " + System.Text.Encoding.Default.GetString(buffer.getBuffer()));
            }

            int messageType = buffer.parseInt32(HEADER_TYPE_OFFSET);
			BSPMessageType type = BSPMessageType.UNKNOWN;
            if (Enum.IsDefined(typeof(BSPMessageType), messageType)) {
                type = (BSPMessageType)messageType;
            }

            int sourceID = buffer.parseInt32(HEADER_SOURCE_OFFSET);

            int bytesRemaining = buffer.size() - HEADER_LENGTH;
			NetworkBuffer data = new NetworkBuffer(bytesRemaining);

            if (bytesRemaining > 0) {
                data.writeBytes(buffer.getBuffer(), HEADER_LENGTH, 0, bytesRemaining);
            }

			return new BSPMessage(message.address, type, sourceID, data);
        }

		private class AgentConnection : INetworkConnection
		{
			private readonly BeliefSyncProtocol protocol;
            private readonly int agentID;
			private IPEndPoint agentAddress;

			public AgentConnection(BeliefSyncProtocol protocol, int agentID)
			{
				this.protocol = protocol;
                this.agentID = agentID;
			}

			public void setRemoteAddress(IPEndPoint address)
			{
				this.agentAddress = address;
			}

			public IPEndPoint getRemoteAddress()
			{
				return agentAddress;
			}

			public void send(BSPMessage bspMessage)
			{
				Message message = protocol.formatMessage(bspMessage);
				protocol.messageWriter.write(message);
			}
		}
	}
}

