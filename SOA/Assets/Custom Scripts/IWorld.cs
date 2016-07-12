using System;
using System.Collections.Generic;

namespace soa
{

	public enum Affiliation { BLUE = 0, RED = 1, NEUTRAL = 2, UNCLASSIFIED = 3 };

	public interface IWorld
	{
        IEnumerable<ISoaActor> getActors();

        IEnumerable<ISoaJammer> getJammers();	
	}
}