﻿#region Licence
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

namespace bzPSD
{
    /// <summary>
    /// Writes primitive data types as binary values in in big-endian format
    /// </summary>
    public sealed class BinaryReverseWriter : BinaryWriter
    {
        public bool AutoFlush { get; set; }

        public BinaryReverseWriter(Stream stream)
            : base(stream)
        {

        }

        public void WritePascalString(string s)
        {
            char[] c = s.Length > 255 ? s.Substring(0, 255).ToCharArray() : s.ToCharArray();

            Write((byte)c.Length);
            Write(c);

            int realLength = c.Length + 1;

            if (realLength % 2 == 0) return;

            for (int i = 0; i < 2 - realLength % 2; i++)
            {
                Write((byte)0);
            }

            if (AutoFlush) Flush();
        }

        public override void Write(short val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }

        public override void Write(int val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }

        public override void Write(long val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }

        public override void Write(ushort val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }

        public override void Write(uint val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }

        public override void Write(ulong val)
        {
            val = Utilities.SwapBytes(val);
            base.Write(val);

            if (AutoFlush) Flush();
        }
    }
}