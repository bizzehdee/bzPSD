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

namespace bzPSD
{
    /// <summary>
    /// Parsed image resource 1036 or 1033 — the embedded JPEG thumbnail.
    /// </summary>
    public sealed class Thumbnail : ImageResource
    {
        /// <summary>
        /// Thumbnail width in pixels (from the resource header).
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Thumbnail height in pixels (from the resource header).
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Raw JPEG bytes when the thumbnail is stored in JPEG format (format code 1).
        /// Pass directly to a JPEG decoder of your choice (e.g. <c>SixLabors.ImageSharp</c>,
        /// <c>SkiaSharp</c>, or <c>System.Drawing</c> on Windows).
        /// Null when the thumbnail uses the raw-pixel format.
        /// </summary>
        public byte[] JpegData { get; }

        public Thumbnail(ImageResource imageResource)
            : base(imageResource)
        {
            using (BinaryReverseReader reader = DataReader)
            {
                int format           = reader.ReadInt32();
                Width                = reader.ReadInt32();
                Height               = reader.ReadInt32();
                reader.ReadInt32();  // widthBytes (stride)
                reader.ReadInt32();  // uncompressed size
                reader.ReadInt32();  // compressed size
                reader.ReadInt16();  // bits per pixel
                reader.ReadInt16();  // planes

                if (format == 1)
                {
                    JpegData = reader.ReadBytes(
                        (int)(reader.BaseStream.Length - reader.BaseStream.Position));
                }
                // format != 1 is an undocumented raw-pixel variant; pixel data is not
                // parsed here since the format has no public specification.
            }
        }
    }
}
