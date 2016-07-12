using System;

namespace soa
{
	public interface ISoaJammer
	{
		ISoaActor getActor();
		
		float getEffectiveRangeKm();
	}
}