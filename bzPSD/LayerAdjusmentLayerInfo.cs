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

using bzPSD;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Drawing.PSD
{
    public partial class Layer
    {
        public class AdjusmentLayerInfo
        {
            public AdjusmentLayerInfo(string key, Layer layer)
            {
                Key = key;
                Layer = layer;
                Layer.AdjustmentInfo.Add(this);
            }

            public AdjusmentLayerInfo(BinaryReverseReader reader, Layer layer)
            {
                Debug.WriteLine("AdjusmentLayerInfo started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

                Layer = layer;

                ReadOnlySpan<char> signature = reader.ReadChars(4);

                if (!signature.SequenceEqual("8BIM".AsSpan()))
                {
                    throw new IOException("Could not read an image resource");
                }

                Key = new string(reader.ReadChars(4));

                uint dataLength = reader.ReadUInt32();

                Data = reader.ReadBytes((int)dataLength);
            }

            /// <summary>
            /// The layer to which this info belongs
            /// </summary>
            private Layer Layer { get; set; }

            public string Key { get; }

            public byte[] Data { get; }

            public void Save(BinaryReverseWriter writer)
            {
                Debug.WriteLine("AdjusmentLayerInfo Save started at " + writer.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

                const string signature = "8BIM";

                writer.Write(signature.ToCharArray());
                writer.Write(Key.ToCharArray());
                writer.Write((uint)Data.Length);
                writer.Write(Data);
            }

            public BinaryReverseReader DataReader
            {
                get => new BinaryReverseReader(new MemoryStream(Data));
            }
        }

    }
}
