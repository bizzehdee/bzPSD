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
    /// Color space identifier used in <see cref="ChannelDisplayInfo"/>.
    /// </summary>
    public enum ChannelColorSpace : short
    {
        RGB       = 0,
        HSB       = 1,
        CMYK      = 2,
        Pantone   = 3,
        Focoltone = 4,
        Trumatch  = 5,
        Toyo      = 6,
        Lab       = 7,
        Grayscale = 8,
        HKS       = 10,
    }

    /// <summary>
    /// How the channel is used in the Channels panel.
    /// </summary>
    public enum ChannelKind : byte
    {
        /// <summary>Normal colour channel (selected by default).</summary>
        Selected  = 0,
        /// <summary>Colour channel that is protected from editing.</summary>
        Protected = 1,
        /// <summary>Spot colour channel (ink / duotone ink).</summary>
        Spot      = 2,
    }

    /// <summary>
    /// Display information for a single channel from resource 1007.
    /// </summary>
    public sealed class ChannelDisplayInfo
    {
        /// <summary>Color space for <see cref="ColorComponents"/>.</summary>
        public ChannelColorSpace ColorSpace { get; }

        /// <summary>
        /// Four raw color component values. Interpretation depends on <see cref="ColorSpace"/>:
        /// RGB 0–65535 per component; CMYK 0–10000 (= 0–100 %); Lab L 0–10000, a/b –12800–12700.
        /// Unused components are zero.
        /// </summary>
        public ushort[] ColorComponents { get; }

        /// <summary>Channel opacity, 0–100.</summary>
        public short Opacity { get; }

        /// <summary>Whether this channel is a normal, protected, or spot channel.</summary>
        public ChannelKind Kind { get; }

        internal ChannelDisplayInfo(BinaryReverseReader reader, bool hasPadding)
        {
            ColorSpace = (ChannelColorSpace)reader.ReadInt16();
            ColorComponents = new ushort[4];
            for (int i = 0; i < 4; i++)
                ColorComponents[i] = reader.ReadUInt16();
            Opacity = reader.ReadInt16();
            Kind = (ChannelKind)reader.ReadByte();
            if (hasPadding)
                reader.ReadByte();
        }

        internal void Save(BinaryReverseWriter writer, bool hasPadding)
        {
            writer.Write((short)ColorSpace);
            foreach (ushort c in ColorComponents)
                writer.Write(c);
            writer.Write(Opacity);
            writer.Write((byte)Kind);
            if (hasPadding)
                writer.Write((byte)0);
        }
    }

    /// <summary>
    /// Parsed image resource 1007 — per-channel display information.
    /// Channels are ordered as they appear in Photoshop's Channels panel:
    /// composite first, then individual colour channels, then any extra
    /// (alpha / spot) channels.
    /// </summary>
    public sealed class DisplayInfo : ImageResource
    {
        // PS 5.5+ uses 14 bytes per entry (13 + 1 padding); earlier versions use 13.
        private readonly bool _hasPadding;

        public IReadOnlyList<ChannelDisplayInfo> Channels { get; }

        public DisplayInfo(ImageResource imageResource) : base(imageResource)
        {
            var channels = new List<ChannelDisplayInfo>();

            using (BinaryReverseReader reader = DataReader)
            {
                int dataLen = (int)reader.BaseStream.Length;
                _hasPadding = dataLen % 14 == 0;
                int entrySize = _hasPadding ? 14 : 13;
                int count = dataLen / entrySize;

                for (int i = 0; i < count; i++)
                    channels.Add(new ChannelDisplayInfo(reader, _hasPadding));
            }

            Channels = channels;
        }

        protected override void StoreData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryReverseWriter(ms);
            foreach (var ch in Channels)
                ch.Save(writer, _hasPadding);
            Data = ms.ToArray();
        }
    }
}
