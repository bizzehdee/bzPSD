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

using System.Collections.Generic;
using System.IO;

namespace bzPSD
{
    /// <summary>
    /// Parsed image resource 1045 — Unicode names of the extra (alpha / spot) channels.
    /// Each name is stored as a 4-byte character count followed by UTF-16 BE characters.
    /// Prefer this resource over <see cref="AlphaChannels"/> (resource 1006) when present,
    /// as it supports the full Unicode character set.
    /// </summary>
    public sealed class UnicodeAlphaNames : ImageResource
    {
        private readonly List<string> _channelNames = new List<string>();

        public UnicodeAlphaNames()
            : base((short)ResourceIDs.UnicodeAlphaNames)
        {
        }

        public UnicodeAlphaNames(ImageResource imageResource)
            : base(imageResource)
        {
            using (BinaryReverseReader reader = DataReader)
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
                {
                    int charCount = reader.ReadInt32();
                    if (charCount <= 0) break;

                    var chars = new char[charCount];
                    for (int i = 0; i < charCount; i++)
                        chars[i] = (char)reader.ReadUInt16();

                    _channelNames.Add(new string(chars));
                }
            }
        }

        /// <summary>
        /// Channel names in file order (matches the extra-channel ordering used by
        /// <see cref="AlphaChannels"/> and <see cref="DisplayInfo"/>).
        /// </summary>
        public IEnumerable<string> ChannelNames => _channelNames;

        protected override void StoreData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryReverseWriter(ms);
            foreach (string name in _channelNames)
            {
                writer.Write(name.Length);
                foreach (char c in name)
                    writer.Write((ushort)c);
            }
            Data = ms.ToArray();
        }
    }
}
