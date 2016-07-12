using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class CacheKey : IComparable<CacheKey>
    {
        private Belief.Key key;
        private int id;

        public CacheKey(Belief.Key key, int id)
        {
            this.key = key;
            this.id = id;
        }
        
        public Belief.BeliefType getBeliefType()
        {
            return key.getType();
        }

        public int getCustomType()
        {
            return key.getCustomType();
        }

        public int getBeliefID()
        {
            return id;
        }

        public int CompareTo(CacheKey other)
        {
            int comparison = key.CompareTo(other.key);
            if (comparison == 0)
            {
                return id.CompareTo(other.id);
            }
            return comparison;
        }

        public override int GetHashCode()
        {
            int hashCode = key.GetHashCode();
            return hashCode * 100 + id;
        }

        public override bool Equals(object obj)
        {
            CacheKey other = obj as CacheKey;
            if (other == null)
                return false;

            return other.key.Equals(key)
                    && other.id == id;
        }

        public override string ToString()
        {
            return "key=" + key + " id=" + id;
        }
    };
}
