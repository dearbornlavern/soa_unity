using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace soa
{
    public class SHA1_Hash : IHashFunction
    {
        public Hash generateHash(byte[] data)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                return new Hash(sha1.ComputeHash(data));
            }
        }
    }
}
