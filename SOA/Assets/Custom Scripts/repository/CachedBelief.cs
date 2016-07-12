using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class CachedBelief
    {
        private readonly Belief belief;
        private readonly byte[] serialized;
        private readonly Hash hash;

        public CachedBelief(Belief belief, byte[] serialized, Hash hash)
        {
            this.belief = belief;
            this.serialized = serialized;
            this.hash = hash;
        }

        public Belief GetBelief()
        {
            return belief;
        }

        public byte[] GetSerializedBelief()
        {
            return serialized;
        }

        public Hash GetHash()
        {
            return hash;
        }
    }
}
