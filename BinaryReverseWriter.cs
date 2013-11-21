#region Licence
/*
Copyright (c) 2013, Darren Horrocks
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
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