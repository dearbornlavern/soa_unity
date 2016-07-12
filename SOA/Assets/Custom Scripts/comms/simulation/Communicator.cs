using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{

    public delegate void CommunicatorCallback<AddressType>(Belief belief, AddressType address);

    public interface Communicator<AddressType>
    {

        int GetBandwidth();
        
        void Broadcast(IEnumerable<Belief> beliefs);

        void Send(Belief belief, AddressType address);

        void RegisterCallback(CommunicatorCallback<AddressType> callback);

    }
}
