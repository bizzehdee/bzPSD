namespace System.Drawing.PSD
{
	public class Utilities
	{
		public static UInt16 SwapBytes(UInt16 x)
		{
			return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
		}

		public static UInt32 SwapBytes(UInt32 x)
		{
			// swap adjacent 16-bit blocks
			x = (x >> 16) | (x << 16);
			// swap adjacent 8-bit blocks
			return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
		}

		public static UInt64 SwapBytes(UInt64 x)
		{
			// swap adjacent 32-bit blocks
			x = (x >> 32) | (x << 32);
			// swap adjacent 16-bit blocks
			x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
			// swap adjacent 8-bit blocks
			return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
		}

		public static Int16 SwapBytes(Int16 x)
		{
			return (Int16)SwapBytes((UInt16)x);
		}

		public static Int32 SwapBytes(Int32 x)
		{
			return (Int32)SwapBytes((UInt32)x);
		}

		public static Int64 SwapBytes(Int64 x)
		{
			return (Int64)SwapBytes((UInt64)x);
		}
	}
}
