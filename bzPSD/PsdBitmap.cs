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

using System;
using System.Drawing;

namespace bzPSD
{
    /// <summary>
    /// A platform-independent RGBA pixel buffer returned by <see cref="ImageDecoder"/>.
    ///
    /// Pixel layout: <see cref="Pixels"/> is a flat byte array in row-major order,
    /// 4 bytes per pixel (R, G, B, A).  Index arithmetic:
    /// <code>
    ///   int i = (y * Width + x) * 4;
    ///   byte r = Pixels[i], g = Pixels[i+1], b = Pixels[i+2], a = Pixels[i+3];
    /// </code>
    ///
    /// Converting to a third-party type (examples):
    /// <code>
    ///   // SkiaSharp
    ///   var skBmp = new SKBitmap(bmp.Width, bmp.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    ///   System.Runtime.InteropServices.Marshal.Copy(bmp.Pixels, 0, skBmp.GetPixels(), bmp.Pixels.Length);
    ///
    ///   // ImageSharp
    ///   var img = Image.LoadPixelData&lt;Rgba32&gt;(bmp.Pixels, bmp.Width, bmp.Height);
    ///
    ///   // WPF WriteableBitmap (expects BGR order — swap R and B first)
    /// </code>
    /// </summary>
    public sealed class PsdBitmap : IDisposable
    {
        /// <summary>Width in pixels.</summary>
        public int Width { get; }

        /// <summary>Height in pixels.</summary>
        public int Height { get; }

        /// <summary>
        /// Raw RGBA pixel data, 4 bytes per pixel, row-major.
        /// Initialised to all zeros (transparent black).
        /// </summary>
        public byte[] Pixels { get; }

        public PsdBitmap(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            Pixels = new byte[width * height * 4];
        }

        /// <summary>Returns the colour of the pixel at (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public Color GetPixel(int x, int y)
        {
            int i = (y * Width + x) * 4;
            return Color.FromArgb(Pixels[i + 3], Pixels[i], Pixels[i + 1], Pixels[i + 2]);
        }

        /// <summary>Sets the colour of the pixel at (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public void SetPixel(int x, int y, Color c)
        {
            int i = (y * Width + x) * 4;
            Pixels[i]     = c.R;
            Pixels[i + 1] = c.G;
            Pixels[i + 2] = c.B;
            Pixels[i + 3] = c.A;
        }

        // No unmanaged resources; IDisposable is implemented so that callers can
        // use 'using (var bmp = ImageDecoder.DecodeImage(...))' without changes.
        public void Dispose() { }
    }
}
