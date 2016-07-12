using System;

namespace soa
{
	public class PositionKM
	{
		private readonly float x;
		private readonly float z;
		private readonly float altitude;

		public PositionKM(float x, float altitude, float z)
		{
			this.x = x;
			this.altitude = altitude;
			this.z = z;
		}

		public float getX()
		{
			return x;
		}

		public float getZ()
		{
			return z;
		}

		public float getAltitude()
		{
			return altitude;
		}

		public float distance(PositionKM other)
		{
			float dX = x - other.x;
			float dY = altitude - other.altitude;
			float dZ = z - other.z;
			return (float)Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
		}
	}
}

