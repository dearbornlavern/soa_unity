using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public interface ICommManager
    {
        void start();

        void terminate();

        string getConnectionForAgent(int agentID);

		void addNewActor(int actorID, BeliefRepository repo);
    }
}
