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