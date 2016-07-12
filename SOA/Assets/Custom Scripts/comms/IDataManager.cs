using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public interface IDataManager
    {
        List<Belief> getInitializationBeliefs();

        void addExternalBeliefToActor(Belief b, int sourceId);
    }
}
