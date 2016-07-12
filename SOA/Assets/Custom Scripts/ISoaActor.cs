using System;
using System.Collections.Generic;

namespace soa
{
	public interface ISoaActor
	{
		int getID();

		PositionKM getPositionInKilometers();

		bool isBalloon();

		bool isBaseStation();

		float getCommsRangeKM();
	}
}

