using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class Hash : IComparable<Hash>
    {
        private byte[] hashCode;

        public Hash(byte[] hashCode)
        {
            if (hashCode == null)
            {
                throw new Exception("Invalid hash");
            }

            this.hashCode = hashCode;
        }

        public byte[] GetBytes()
        {
            return hashCode;
        }

        public int CompareTo(Hash other)
        {
            if (hashCode.Length != other.hashCode.Length)
            {
                return hashCode.Length.CompareTo(other.hashCode.Length);
            }

            for (int i = 0; i < hashCode.Length; ++i)
            {
                int comparison = hashCode[i].CompareTo(other.hashCode[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            return 0;
        }

        public override int GetHashCode()
        {
            int hash = hashCode[0];
            for (int i = 1; i < hashCode.Length; ++i)
            {
                int shift = i % 4;
                hash ^= (hashCode[i] << shift);
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            Hash other = obj as Hash;
            if (other == null)
                return false;

            if (hashCode.Length != other.hashCode.Length)
                return false;

            bool equal = true;
            for (int i = 0; i < hashCode.Length; ++i)
            {
                equal &= hashCode[i] == other.hashCode[i];
            }
            return equal;
        }

        public override string ToString()
        {
            string hashString = "";
            for (int i = 0; i < hashCode.Length; ++i)
            {
                hashString += String.Format("{0:X2}", hashCode[i]);
            }
            return hashString;
        }
    }
}
