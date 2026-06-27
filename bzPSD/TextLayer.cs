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
    /// Provides read/write access to the text content and transform of a PSD text layer.
    /// Obtained via <see cref="Layer.TextData"/>; check <see cref="Layer.IsTextLayer"/> first.
    ///
    /// Setting <see cref="Text"/> rewrites the TySh block in place while preserving all
    /// other text properties (font, size, colour, paragraph settings, etc.).
    /// Call <see cref="bzPSD.PsdFile.Save(string)"/> after modifying to persist to disk.
    /// </summary>
    public sealed class TextLayer
    {
        private readonly Layer.AdjusmentLayerInfo _info;
        private readonly short _tyshVersion;
        private readonly double[] _transform;  // [xx, xy, yx, yy, tx, ty]
        private readonly short _textDescriptorVersion;
        private readonly bzPSD.PsdDescriptor _descriptor;

        internal TextLayer(Layer.AdjusmentLayerInfo info)
        {
            _info = info;
            using var reader = info.DataReader;

            _tyshVersion = reader.ReadInt16();

            _transform = new double[6];
            for (int i = 0; i < 6; i++)
                _transform[i] = reader.ReadDouble();

            _textDescriptorVersion = reader.ReadInt16();
            _descriptor = bzPSD.PsdDescriptor.Read(reader);
        }

        /// <summary>
        /// The text string displayed by this layer.  Supports the full Unicode range.
        /// Assigning a new value immediately rewrites the in-memory TySh block;
        /// call <c>PsdFile.Save()</c> to persist.
        /// </summary>
        public string Text
        {
            get => (_descriptor["Txt "] as bzPSD.PsdStringValue)?.Value ?? string.Empty;
            set
            {
                if (_descriptor["Txt "] is bzPSD.PsdStringValue sv)
                    sv.Value = value;
                Commit();
            }
        }

        /// <summary>
        /// The 2-D affine transform matrix stored in the TySh block.
        /// Elements are [xx, xy, yx, yy, tx, ty], corresponding to the Photoshop
        /// "transform" descriptor.  tx/ty give the layer's position on the canvas.
        /// </summary>
        public double[] Transform => (double[])_transform.Clone();

        // Reserialises the TySh block back to AdjusmentLayerInfo.Data so that
        // Layer.Save() picks up the change.
        private void Commit()
        {
            using var ms = new MemoryStream();
            using var writer = new bzPSD.BinaryReverseWriter(ms);

            writer.Write(_tyshVersion);
            foreach (double d in _transform)
                writer.Write(d);
            writer.Write(_textDescriptorVersion);
            _descriptor.Write(writer);

            _info.Data = ms.ToArray();
        }
    }
}
