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

namespace bzPSD
{
    public class Utilities
    {
        public static ushort SwapBytes(ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | x >> 8 & 0xff);
        }

        public static uint SwapBytes(uint x)
        {
            // swap adjacent 16-bit blocks
            x = x >> 16 | x << 16;
            // swap adjacent 8-bit blocks
            return (x & 0xFF00FF00) >> 8 | (x & 0x00FF00FF) << 8;
        }

        public static ulong SwapBytes(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = x >> 32 | x << 32;
            // swap adjacent 16-bit blocks
            x = (x & 0xFFFF0000FFFF0000) >> 16 | (x & 0x0000FFFF0000FFFF) << 16;
            // swap adjacent 8-bit blocks
            return (x & 0xFF00FF00FF00FF00) >> 8 | (x & 0x00FF00FF00FF00FF) << 8;
        }

        public static short SwapBytes(short x)
        {
            return (short)SwapBytes((ushort)x);
        }

        public static int SwapBytes(int x)
        {
            return (int)SwapBytes((uint)x);
        }

        public static long SwapBytes(long x)
        {
            return (long)SwapBytes((ulong)x);
        }
    }
}
