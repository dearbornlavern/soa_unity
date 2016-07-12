using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class BeliefRepository
    {
        private SortedDictionary<CacheKey, CachedBelief> beliefCache;
        private Serializer serializer;
        private IHashFunction hashFunction;
        private RepositoryState currentState;

	    public BeliefRepository(Serializer serializer, IHashFunction hashFunction)
        {
            this.beliefCache = new SortedDictionary<CacheKey, CachedBelief>();
            this.serializer = serializer;
            this.hashFunction = hashFunction;
            this.currentState = new RepositoryState(0);
        }
        
        public void SyncWith(BeliefRepository other)
        {
            foreach(CachedBelief belief in Diff(other.CurrentState()))
            {
                other.Commit(belief.GetBelief());
            }

            foreach (CachedBelief belief in other.Diff(CurrentState()))
            {
                other.Commit(belief.GetBelief());
            }
        }

        public IEnumerable<BeliefType> FindAll<BeliefType>(Belief.BeliefType type) where BeliefType : Belief
        {
            return GetAllCachedBeliefs()
                    .Where(belief => belief.GetBelief() as BeliefType != null && belief.GetBelief().getBeliefType() == type)
                    .Select(cached => (BeliefType)cached.GetBelief());
        }

        public BeliefType Find<BeliefType>(Belief.BeliefType type, int id) where BeliefType : Belief
        {
            return Find<BeliefType>(Belief.keyOf(type), id);
        }

        public BeliefType Find<BeliefType>(Belief.Key key, int id) where BeliefType : Belief
        {
            CacheKey cacheKey = new CacheKey(key, id);
            CachedBelief found = null;
            beliefCache.TryGetValue(cacheKey, out found);
            if (found != null)
            {
                return found.GetBelief() as BeliefType;
            }
            return null;
        }

		public IEnumerable<Belief> GetAllBeliefs()
		{
			return GetAllCachedBeliefs().Select(cached => cached.GetBelief());
		}

		public List<CachedBelief> GetAllCachedBeliefs()
        {
            lock(this)
            {
                return beliefCache.Values.ToList();
            }
        }

        public RepositoryState CurrentState()
        {
            return currentState;
        }

        public bool Commit(Belief belief)
        {
            lock (this)
            {
                CacheKey key = new CacheKey(belief.getTypeKey(), belief.getId());

                CachedBelief oldCache = null;
                if (beliefCache.TryGetValue(key, out oldCache))
                {
                    BeliefMerger merger = new BeliefMerger();
                    Belief merged = merger.merge(oldCache.GetBelief(), belief);
                    if (merged == oldCache.GetBelief())
                    {
                        return false;
                    }
                    belief = merged;
                }

                byte[] serialized = serializer.serializeBelief(belief);
                Hash hash = hashFunction.generateHash(serialized);
                CachedBelief cached = new CachedBelief(belief, serialized, hash);
                beliefCache[key] = cached;
                currentState = RegenerateState();
                
                return true;
            }
        }
        
        public IEnumerable<CachedBelief> Diff(RepositoryState state)
        {
            lock (this)
            {
                RepositoryState myState = CurrentState();
                ICollection<CacheKey> diffKeys = myState.Diff(state);

                List<CachedBelief> beliefs = new List<CachedBelief>();
                foreach (CacheKey key in diffKeys)
                {
                    CachedBelief cachedBelief = beliefCache[key];
                    beliefs.Add(cachedBelief);
                }
                return beliefs;
            }
        }
        public RepositoryState RegenerateState()
        {
            lock (this)
            {
				RepositoryState state = new RepositoryState(currentState.RevisionNumber() + 1);
                foreach (KeyValuePair<CacheKey, CachedBelief> entry in beliefCache)
                {
                    state.Add(entry.Key, entry.Value.GetHash());
                }
                return state;
            }
        }
    }
}
