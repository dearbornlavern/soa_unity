using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class Belief
    {

        public static Key keyOf(BeliefType type)
        {
            return new Key(type);
        }

        public class Key : IComparable<Key>
        {
            private int customType;
            private BeliefType type;

            public Key(BeliefType type)
            {
                this.type = type;
                this.customType = 0;
            }

            public Key(int customType) 
            {
                this.type = BeliefType.CUSTOM;
                this.customType = customType;
            }

            public BeliefType getType()
            {
                return type;
            }

            public int getCustomType()
            {
                return customType;
            }

            public int CompareTo(Key other)
            {
                if (type == other.type)
                {
                    return customType.CompareTo(other.customType);
                }

                return type.CompareTo(other.type);
            }

            public override int GetHashCode()
            {
                int hashCode = (int)type;
                hashCode += customType + 100;
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                Key other = obj as Key;
                if (other == null)
                    return false;

                return other.type == type 
                        && other.customType == customType;
            }

            public override string ToString()
            {
                if (type == BeliefType.CUSTOM)
                {
                    return "custom:" + customType;
                }
                return type.ToString();
            }
        };

        public enum BeliefType
        {
            INVALID = 0,
            ACTOR, 
            BASE,
            CASUALTY_DELIVERY,
            CASUALTY_PICKUP,
            GRIDSPEC,
            MODE_COMMAND,
            NGOSITE, 
            ROADCELL, 
            SPOI, 
            SUPPLY_DELIVERY,
            SUPPLY_PICKUP,
            TERRAIN,
            TIME,
            VILLAGE, 
            WAYPOINT, 
            WAYPOINT_OVERRIDE,
            WAYPOINT_PATH,
            CUSTOM
        };

        private static System.DateTime epoch = new System.DateTime(1970, 1, 1);

        public Belief(int id)
        {
            this.id = id;
            beliefTime = (UInt64)(System.DateTime.UtcNow - epoch).Ticks/10000;
        }

        // Each belief must be able to give its type
        public virtual BeliefType getBeliefType()
        {
            return BeliefType.INVALID;
        }

        public virtual Key getTypeKey()
        {
            return new Key(getBeliefType());
        }

        public virtual int getId()
        {
            return id;
        }
        public UInt64 getBeliefTime()
        {
            return beliefTime;
        }

        public void setBeliefTime(UInt64 beliefTime)
        {
            this.beliefTime = beliefTime;
        }

        private UInt64 beliefTime;
        private int id;
    }
}
