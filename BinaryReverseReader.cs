using System.IO;

namespace System.Drawing.PSD
{
	/// <summary>
	/// Reads primitive data types as binary values in in big-endian format
	/// </summary>
	public class BinaryReverseReader : BinaryReader
	{
		public BinaryReverseReader(Stream stream)
			: base(stream)
		{
		}

		public override Int16 ReadInt16()
		{
			return Utilities.SwapBytes(base.ReadInt16());
		}

		public override Int32 ReadInt32()
		{
			return Utilities.SwapBytes(base.ReadInt32());
		}

		public override Int64 ReadInt64()
		{
			return Utilities.SwapBytes(base.ReadInt64());
		}

		public override UInt16 ReadUInt16()
		{
			return Utilities.SwapBytes(base.ReadUInt16());
		}

		public override UInt32 ReadUInt32()
		{
			return Utilities.SwapBytes(base.ReadUInt32());
		}

		public override UInt64 ReadUInt64()
		{
			return Utilities.SwapBytes(base.ReadUInt64());
		}

		public String ReadPascalString()
		{
			Byte stringLength = base.ReadByte();

			Char[] c = base.ReadChars(stringLength);

			if ((stringLength % 2) == 0) base.ReadByte();

			return new String(c);
		}
	}
}