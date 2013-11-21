using System.IO;

namespace System.Drawing.PSD
{
	/// <summary>
	/// Writes primitive data types as binary values in in big-endian format
	/// </summary>
	public class BinaryReverseWriter : BinaryWriter
	{
		public Boolean AutoFlush { get; set; }

		public BinaryReverseWriter(Stream stream)
			: base(stream)
		{

		}

		public void WritePascalString(String s)
		{
			Char[] c = s.Length > 255 ? s.Substring(0, 255).ToCharArray() : s.ToCharArray();

			base.Write((Byte)c.Length);
			base.Write(c);

			Int32 realLength = c.Length + 1;

			if ((realLength % 2) == 0) return;

			for (Int32 i = 0; i < (2 - (realLength % 2)); i++) base.Write((Byte)0);

			if (AutoFlush) Flush();
		}

		public override void Write(Int16 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}

		public override void Write(Int32 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}

		public override void Write(Int64 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}

		public override void Write(UInt16 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}

		public override void Write(UInt32 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}

		public override void Write(UInt64 val)
		{
			val = Utilities.SwapBytes(val);
			base.Write(val);

			if (AutoFlush) Flush();
		}
	}
}