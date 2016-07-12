using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public interface IHashFunction
    {
        Hash generateHash(byte[] data);
    }
}
