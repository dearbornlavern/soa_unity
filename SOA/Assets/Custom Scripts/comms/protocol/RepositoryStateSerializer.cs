using System;
namespace soa
{
	public class RepositoryStateSerializer
	{
        private const int COMMIT_OFFSET     = 0;
        private const int NUM_BELIEFS_OFFSET= 4;
        private const int HEADER_SIZE       = 8;
        private const int TYPE_OFFSET = 0;
        private const int CUSTOM_TYPE_OFFSET = 1;
        private const int BELIEF_ID_OFFSET = 5;
        private const int DIGEST_OFFSET = 9;
        private const int DIGEST_SIZE = 20;
        private const int BELIEF_SIZE = 29;

        public NetworkBuffer serialize(RepositoryState state)
		{
            NetworkBuffer buffer = new NetworkBuffer(HEADER_SIZE + (state.Size() * BELIEF_SIZE));
            buffer.writeInt32(COMMIT_OFFSET, state.RevisionNumber());
            buffer.writeInt32(NUM_BELIEFS_OFFSET, state.Size());

            int offset = HEADER_SIZE;
            foreach (RepositoryState.RepositoryObject belief in state.GetObjects())
            {
                buffer.writeByte(offset + TYPE_OFFSET, (byte)belief.key.getBeliefType());
                buffer.writeInt32(offset + CUSTOM_TYPE_OFFSET, belief.key.getCustomType());
                buffer.writeInt32(offset + BELIEF_ID_OFFSET, belief.key.getBeliefID());

                byte[] hashBytes = belief.hash.GetBytes();
                buffer.writeBytes(hashBytes, 0, offset + DIGEST_OFFSET, hashBytes.Length);
            }

            return buffer;
		}

		public RepositoryState deserialize(NetworkBuffer buffer)
		{
            int revisionNumber = buffer.parseInt32(COMMIT_OFFSET);
            RepositoryState state = new RepositoryState(revisionNumber);

            int numberOfBeliefs = buffer.parseInt32(NUM_BELIEFS_OFFSET);
            int baseOffset = HEADER_SIZE;
            for (int i = 0; i < numberOfBeliefs; ++i, baseOffset += BELIEF_SIZE)
            {
                int typeVal = buffer.readByte(baseOffset + TYPE_OFFSET);
                Belief.BeliefType type = Belief.BeliefType.INVALID;
                if (Enum.IsDefined(typeof(Belief.BeliefType), typeVal))
                {
                    type = (Belief.BeliefType)typeVal;
                }
                else
                {
                    Log.error("Invalid belief type: " + typeVal);
                }

                int customType = buffer.parseInt32(baseOffset + CUSTOM_TYPE_OFFSET);
                int id = buffer.parseInt32(baseOffset + BELIEF_ID_OFFSET);
                byte[] digest = buffer.readBytes(baseOffset + DIGEST_OFFSET, DIGEST_SIZE);
                Hash hash = new Hash(digest);

                Belief.Key key = null;
                if (type == Belief.BeliefType.CUSTOM)
                    key = new Belief.Key(customType);
                else
                    key = new Belief.Key(type);

                state.Add(new CacheKey(key, id), hash);
            }

            return state;
		}
	}
}

