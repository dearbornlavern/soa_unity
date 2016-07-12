using System;
using System.Net;

namespace soa
{
	public class NetworkBuffer
	{

		private byte[] data;

		public NetworkBuffer(byte[] data)
		{
			this.data = data;
		}

		public NetworkBuffer(int bufferLength)
		{
			data = new byte[bufferLength];
		}

		public int size() 
		{
            if (data == null)
                return 0;

			return data.Length;
		}

		public byte[] getBuffer()
		{
			return data;
		}

        public void writeByte(int offset, byte value)
        {
            data[offset] = value;
        }

        public byte readByte(int offset)
        {
            return data[offset];
        }

		public void writeBytes(byte[] source, int sourceIndex, int destIndex, int length)
		{
			System.Buffer.BlockCopy(source, sourceIndex, data, destIndex, length);
		}

        public byte[] readBytes(int offset, int length)
        {
            byte[] subArray = new byte[length];
            System.Buffer.BlockCopy(data, offset, subArray, 0, length);
            return subArray;
        }

		public int parseInt32(int startIndex)
        {
            int value = BitConverter.ToInt32(data, startIndex);
            return IPAddress.NetworkToHostOrder(value);
        }

        public void writeInt32(int startIndex, int value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            byte[] bytes = BitConverter.GetBytes(value);

            for (int i = 0;i < bytes.Length; ++i)
            {
				data[startIndex + i] = bytes[i];
            }
        }
	}
}

